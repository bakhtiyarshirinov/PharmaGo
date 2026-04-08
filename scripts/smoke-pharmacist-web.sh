#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"

API_BASE_URL="${API_BASE_URL:-http://localhost:5122}"
FRONTEND_BASE_URL="${FRONTEND_BASE_URL:-http://localhost:3001}"
BACKEND_HEALTH_URL="${API_BASE_URL}/health"
FRONTEND_HEALTH_URL="${FRONTEND_BASE_URL}/login"
BACKEND_LOG="${TMPDIR:-/tmp}/pharmago-pharmacist-backend.log"
FRONTEND_LOG="${TMPDIR:-/tmp}/pharmago-pharmacist-frontend.log"

BACKEND_PID=""
FRONTEND_PID=""

cleanup() {
  if [[ -n "${FRONTEND_PID}" ]] && kill -0 "${FRONTEND_PID}" >/dev/null 2>&1; then
    kill "${FRONTEND_PID}" >/dev/null 2>&1 || true
    wait "${FRONTEND_PID}" >/dev/null 2>&1 || true
  fi

  if [[ -n "${BACKEND_PID}" ]] && kill -0 "${BACKEND_PID}" >/dev/null 2>&1; then
    kill "${BACKEND_PID}" >/dev/null 2>&1 || true
    wait "${BACKEND_PID}" >/dev/null 2>&1 || true
  fi
}

trap cleanup EXIT

require_tool() {
  if ! command -v "$1" >/dev/null 2>&1; then
    echo "Required tool is missing: $1" >&2
    exit 1
  fi
}

wait_for_url() {
  local url="$1"
  local attempts="${2:-30}"
  local delay_seconds="${3:-1}"

  for ((i = 1; i <= attempts; i++)); do
    if curl -fsS "${url}" >/dev/null 2>&1; then
      return 0
    fi

    sleep "${delay_seconds}"
  done

  return 1
}

assert_status_200() {
  local url="$1"
  local cookie_jar="${2:-}"
  local header="${3:-}"
  local status

  if [[ -n "${cookie_jar}" ]]; then
    status="$(curl -s -o /dev/null -w '%{http_code}' -b "${cookie_jar}" -c "${cookie_jar}" "${url}")"
  elif [[ -n "${header}" ]]; then
    status="$(curl -s -o /dev/null -w '%{http_code}' -H "${header}" "${url}")"
  else
    status="$(curl -s -o /dev/null -w '%{http_code}' "${url}")"
  fi

  if [[ "${status}" != "200" ]]; then
    echo "Expected 200 from ${url}, got ${status}" >&2
    exit 1
  fi
}

require_tool curl
require_tool jq
require_tool dotnet
require_tool npm

echo "[1/6] Bootstrapping clean pharmacist smoke data..."
"${SCRIPT_DIR}/bootstrap-pharmacist-smoke.sh" >/dev/null

echo "[2/6] Ensuring backend is running..."
if ! wait_for_url "${BACKEND_HEALTH_URL}" 1 1; then
  dotnet build "${ROOT_DIR}/backend/PharmaGo.Api/PharmaGo.Api.csproj" >/dev/null
  (
    cd "${ROOT_DIR}"
    dotnet run --project backend/PharmaGo.Api --no-build >"${BACKEND_LOG}" 2>&1
  ) &
  BACKEND_PID="$!"

  if ! wait_for_url "${BACKEND_HEALTH_URL}" 60 1; then
    echo "Backend did not become healthy. Log: ${BACKEND_LOG}" >&2
    exit 1
  fi
fi

echo "[3/6] Running backend smoke..."
LOGIN_RESPONSE="$(
  curl -fsS -X POST "${API_BASE_URL}/api/auth/login" \
    -H 'Content-Type: application/json' \
    -d '{"phoneNumber":"+994509990001","password":"Pharmacist123!"}'
)"

ACCESS_TOKEN="$(printf '%s' "${LOGIN_RESPONSE}" | jq -r '.accessToken')"
PHARMACY_ID="$(printf '%s' "${LOGIN_RESPONSE}" | jq -r '.user.pharmacyId')"

if [[ -z "${ACCESS_TOKEN}" || "${ACCESS_TOKEN}" == "null" ]]; then
  echo "Backend login failed: access token missing." >&2
  exit 1
fi

AUTH_HEADER="Authorization: Bearer ${ACCESS_TOKEN}"

ME_RESPONSE="$(curl -fsS "${API_BASE_URL}/api/auth/me" -H "${AUTH_HEADER}")"
ME_PHONE="$(printf '%s' "${ME_RESPONSE}" | jq -r '.phoneNumber')"

if [[ "${ME_PHONE}" != "+994509990001" ]]; then
  echo "Backend me endpoint returned unexpected user: ${ME_PHONE}" >&2
  exit 1
fi

DASHBOARD_SUMMARY="$(curl -fsS "${API_BASE_URL}/api/dashboard/summary" -H "${AUTH_HEADER}")"
ACTIVE_RESERVATIONS="$(printf '%s' "${DASHBOARD_SUMMARY}" | jq -r '.activeReservations')"

if [[ "${ACTIVE_RESERVATIONS}" != "2" ]]; then
  echo "Unexpected active reservation count in dashboard: ${ACTIVE_RESERVATIONS}" >&2
  exit 1
fi

QUEUE_RESPONSE="$(curl -fsS "${API_BASE_URL}/api/reservations/pharmacy/${PHARMACY_ID}" -H "${AUTH_HEADER}")"
QUEUE_COUNT="$(printf '%s' "${QUEUE_RESPONSE}" | jq 'length')"
READY_RESERVATION_ID="$(printf '%s' "${QUEUE_RESPONSE}" | jq -r 'map(select(.status == 3)) | .[0].reservationId')"

if [[ "${QUEUE_COUNT}" != "2" || -z "${READY_RESERVATION_ID}" || "${READY_RESERVATION_ID}" == "null" ]]; then
  echo "Reservation queue bootstrap data is incomplete." >&2
  exit 1
fi

curl -fsS "${API_BASE_URL}/api/reservations/${READY_RESERVATION_ID}" -H "${AUTH_HEADER}" >/dev/null
TIMELINE_COUNT="$(
  curl -fsS "${API_BASE_URL}/api/reservations/${READY_RESERVATION_ID}/timeline" -H "${AUTH_HEADER}" | jq 'length'
)"

if [[ "${TIMELINE_COUNT}" -lt 3 ]]; then
  echo "Reservation timeline is shorter than expected: ${TIMELINE_COUNT}" >&2
  exit 1
fi

STOCK_COUNT="$(
  curl -fsS "${API_BASE_URL}/api/stocks/pharmacy/${PHARMACY_ID}" -H "${AUTH_HEADER}" | jq 'length'
)"
LOW_STOCK_COUNT="$(
  curl -fsS "${API_BASE_URL}/api/stocks/alerts/low-stock?pharmacyId=${PHARMACY_ID}" -H "${AUTH_HEADER}" | jq 'length'
)"
NOTIFICATION_UNREAD_COUNT="$(
  curl -fsS "${API_BASE_URL}/api/notifications/unread" -H "${AUTH_HEADER}" | jq -r '.unreadCount'
)"

if [[ "${STOCK_COUNT}" != "3" ]]; then
  echo "Unexpected stock item count: ${STOCK_COUNT}" >&2
  exit 1
fi

if [[ "${LOW_STOCK_COUNT}" != "1" ]]; then
  echo "Unexpected low-stock count: ${LOW_STOCK_COUNT}" >&2
  exit 1
fi

if [[ "${NOTIFICATION_UNREAD_COUNT}" != "2" ]]; then
  echo "Unexpected unread notification count: ${NOTIFICATION_UNREAD_COUNT}" >&2
  exit 1
fi

curl -fsS "${API_BASE_URL}/api/notifications/history?page=1&pageSize=12" -H "${AUTH_HEADER}" >/dev/null
curl -fsS "${API_BASE_URL}/api/notifications/preferences" -H "${AUTH_HEADER}" >/dev/null

echo "[4/6] Ensuring pharmacist-web is running..."
if ! wait_for_url "${FRONTEND_HEALTH_URL}" 1 1; then
  (
    cd "${ROOT_DIR}/frontend"
    npm run build --workspace=@pharmago/pharmacist-web >/dev/null
    npm run start --workspace=@pharmago/pharmacist-web >"${FRONTEND_LOG}" 2>&1
  ) &
  FRONTEND_PID="$!"

  if ! wait_for_url "${FRONTEND_HEALTH_URL}" 60 1; then
    echo "pharmacist-web did not become ready. Log: ${FRONTEND_LOG}" >&2
    exit 1
  fi
fi

echo "[5/6] Running pharmacist-web smoke..."
COOKIE_JAR="$(mktemp)"

FRONTEND_LOGIN_RESPONSE="$(
  curl -fsS -c "${COOKIE_JAR}" -b "${COOKIE_JAR}" -X POST "${FRONTEND_BASE_URL}/api/auth/login" \
    -H 'Content-Type: application/json' \
    -d '{"phoneNumber":"+994509990001","password":"Pharmacist123!"}'
)"

FRONTEND_PHONE="$(printf '%s' "${FRONTEND_LOGIN_RESPONSE}" | jq -r '.user.phoneNumber')"
if [[ "${FRONTEND_PHONE}" != "+994509990001" ]]; then
  echo "Frontend login returned unexpected user: ${FRONTEND_PHONE}" >&2
  exit 1
fi

FRONTEND_SESSION_RESPONSE="$(curl -fsS -c "${COOKIE_JAR}" -b "${COOKIE_JAR}" "${FRONTEND_BASE_URL}/api/auth/session")"
SESSION_PHONE="$(printf '%s' "${FRONTEND_SESSION_RESPONSE}" | jq -r '.user.phoneNumber')"

if [[ "${SESSION_PHONE}" != "+994509990001" ]]; then
  echo "Frontend session returned unexpected user: ${SESSION_PHONE}" >&2
  exit 1
fi

assert_status_200 "${FRONTEND_BASE_URL}/cockpit" "${COOKIE_JAR}"
assert_status_200 "${FRONTEND_BASE_URL}/reservations" "${COOKIE_JAR}"
assert_status_200 "${FRONTEND_BASE_URL}/reservations/${READY_RESERVATION_ID}" "${COOKIE_JAR}"
assert_status_200 "${FRONTEND_BASE_URL}/inventory" "${COOKIE_JAR}"
assert_status_200 "${FRONTEND_BASE_URL}/notifications" "${COOKIE_JAR}"

rm -f "${COOKIE_JAR}"

echo "[6/6] Smoke complete."
echo "Bootstrap credentials: +994509990001 / Pharmacist123!"
echo "Pharmacy ID: ${PHARMACY_ID}"
echo "Ready reservation: ${READY_RESERVATION_ID}"

#!/usr/bin/env bash
set -euo pipefail

# Template for local development. Do NOT commit real secrets.
# Copy to `scripts/export-secrets.sh` and fill in values locally:
#   cp scripts/export-secrets.template.sh scripts/export-secrets.sh
#   source scripts/export-secrets.sh

export TWILIO_PHONE_NUMBER=''
export TWILIO_AUTH_TOKEN=''
export TWILIO_ACCOUNT_SID=''

export BREVO_SENDER=''
export BREVO_FROM_NAME=''
export BREVO_FROM_EMAIL=''
export BREVO_API_KEY=''

export ASPNETCORE_ENVIRONMENT='Development'
export ACCESS_TOKEN_KEY=''

# If using the ASP.NET Core dev cert via env var:
export KESTREL_CERTIFICATES_DEVELOPMENT_PASSWORD=''

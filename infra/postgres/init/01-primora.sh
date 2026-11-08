#!/bin/sh
set -eu
app_password="$(cat /run/secrets/primora_app_password)"
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname postgres --set=app_password="$app_password" <<'SQL'
CREATE ROLE primora_app LOGIN NOSUPERUSER NOCREATEDB NOCREATEROLE PASSWORD :'app_password';
CREATE DATABASE primora OWNER primora_app;
REVOKE ALL ON DATABASE primora FROM PUBLIC;
GRANT CONNECT ON DATABASE primora TO primora_app;
SQL

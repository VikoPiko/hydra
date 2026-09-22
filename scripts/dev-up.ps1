param([switch]$Build)

if ($Build) { docker compose up --build }
else { docker compose up }

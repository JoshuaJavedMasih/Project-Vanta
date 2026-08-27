# Vanta Privacy Notes

Vanta is designed as a local-first monitoring application.

## Data collected locally

Vanta reads supported operating-system telemetry such as CPU activity, physical-memory pressure, fixed-drive capacity, active-interface throughput, process activity, uptime, and available hardware identity information.

## Data not collected

- No advertising identifier
- No personal analytics
- No browser history
- No keystroke or clipboard capture
- No packet contents
- No cloud account information
- No automatic upload of diagnostic reports

## Exports

The JSON export intentionally omits username, computer name, IP addresses, MAC addresses, and hardware serial numbers. Users choose the export destination through the native Windows save picker.

## Permissions

Vanta runs as a standard user. Phase 1 does not request permanent administrator access and does not terminate processes or modify services, startup entries, drivers, firmware, or hardware settings.

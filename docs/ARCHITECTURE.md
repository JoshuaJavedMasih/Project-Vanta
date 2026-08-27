# Vanta Architecture

Vanta separates native collection from interface code so the application remains testable, responsive, and extensible.

## Data flow

```text
ITelemetryProvider
  ├── WindowsTelemetryProvider
  └── SimulatedTelemetryProvider
             │
             ▼
      MonitoringService
      - background loop
      - one-second cadence
      - sensor fault isolation
             │
             ▼
      TelemetrySnapshot
      - immutable domain data
             │
             ▼
     DashboardViewModel
      - display formatting
      - detail-section state
      - observable collections
             │
             ▼
        MainPage.xaml
      - WinUI 3 presentation
      - navigation and charts
```

## Projects

- `Vanta`: WinUI 3 executable application.
- `Vanta.Tests`: MSTest coverage for providers, monitoring, and presentation formatting.

## Provider boundary

`ITelemetryProvider` is the only contract the monitoring loop needs. The native provider collects supported Windows signals; the simulated provider supplies deterministic development data. Future LibreHardwareMonitor, SMART/NVMe, DXGI, battery, or history implementations can be added behind this boundary.

## Threading

`MonitoringService` runs collection outside the UI thread and publishes `TelemetrySnapshot` instances. `MainPage` marshals updates through `DispatcherQueue` before changing observable state or chart geometry.

## Reliability rules

- One failed sensor must not stop the monitoring loop.
- Unavailable values remain unavailable.
- Hardware collection never occurs directly in XAML or view models.
- Phase 1 system actions are read-only.
- Exports exclude private machine identifiers by default.

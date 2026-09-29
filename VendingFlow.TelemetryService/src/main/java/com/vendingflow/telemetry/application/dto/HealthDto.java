package com.vendingflow.telemetry.application.dto;

import java.time.OffsetDateTime;

import com.vendingflow.telemetry.domain.enums.MachineHealthState;
import com.vendingflow.telemetry.domain.enums.TelemetryComponentStatus;

public record HealthDto(String machineId, MachineHealthState state, OffsetDateTime lastHeartbeatAt,
		TelemetryComponentStatus cashModuleStatus, TelemetryComponentStatus dispenserStatus,
		TelemetryComponentStatus doorStatus, Integer networkStrength) {
}

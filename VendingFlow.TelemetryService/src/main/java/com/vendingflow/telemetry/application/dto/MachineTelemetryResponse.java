package com.vendingflow.telemetry.application.dto;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

import com.vendingflow.telemetry.domain.enums.MachineHealthState;
import com.vendingflow.telemetry.domain.enums.TelemetryComponentStatus;
import com.vendingflow.telemetry.domain.model.MachineTelemetry;

public record MachineTelemetryResponse(Long machineTelemetryId, String machineId, OffsetDateTime recordedAt,
		BigDecimal temperature, Integer networkStrength, TelemetryComponentStatus cashModuleStatus,
		TelemetryComponentStatus dispenserStatus, TelemetryComponentStatus doorStatus, MachineHealthState healthState,
		String softwareVersion) {

	public static MachineTelemetryResponse from(MachineTelemetry telemetry) {
		return new MachineTelemetryResponse(telemetry.getMachineTelemetryId(), telemetry.getMachineId(),
				telemetry.getRecordedAt(), telemetry.getTemperature(), telemetry.getNetworkStrength(),
				telemetry.getCashModuleStatus(), telemetry.getDispenserStatus(), telemetry.getDoorStatus(),
				telemetry.getHealthState(), telemetry.getSoftwareVersion());
	}
}

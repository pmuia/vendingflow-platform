package com.vendingflow.telemetry.domain.model;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

import org.junit.jupiter.api.Test;

import com.vendingflow.telemetry.application.dto.HeartbeatDto;
import com.vendingflow.telemetry.domain.enums.DoorStatus;
import com.vendingflow.telemetry.domain.enums.MachineHealthState;

class MachineTelemetryTest {

	@Test
	void closedDoorProducesOnlineTelemetry() {
		var telemetry = MachineTelemetry.fromHeartbeat(heartbeat("OK", "CLOSED", 88));

		assertEquals(DoorStatus.CLOSED, telemetry.getDoorStatus());
		assertEquals(MachineHealthState.ONLINE, telemetry.getHealthState());
	}

	@Test
	void openDoorProducesDegradedTelemetry() {
		var telemetry = MachineTelemetry.fromHeartbeat(heartbeat("OK", "OPEN", 88));

		assertEquals(DoorStatus.OPEN, telemetry.getDoorStatus());
		assertEquals(MachineHealthState.DEGRADED, telemetry.getHealthState());
	}

	@Test
	void dispenserErrorProducesDegradedTelemetry() {
		var telemetry = MachineTelemetry.fromHeartbeat(heartbeat("ERROR", "CLOSED", 88));

		assertEquals(MachineHealthState.DEGRADED, telemetry.getHealthState());
	}

	private HeartbeatDto heartbeat(String dispenserStatus, String doorStatus, int networkStrength) {
		return new HeartbeatDto("VM-001", OffsetDateTime.now(), new BigDecimal("4.30"), networkStrength,
				"OK", dispenserStatus, doorStatus, "1.0.0");
	}
}

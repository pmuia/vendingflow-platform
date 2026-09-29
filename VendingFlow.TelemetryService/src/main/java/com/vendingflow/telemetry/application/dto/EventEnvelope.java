package com.vendingflow.telemetry.application.dto;

import java.time.OffsetDateTime;

import com.fasterxml.jackson.databind.JsonNode;

public record EventEnvelope(String eventId, String eventType, OffsetDateTime timestamp, String correlationId,
		String machineId, JsonNode payload) {
}

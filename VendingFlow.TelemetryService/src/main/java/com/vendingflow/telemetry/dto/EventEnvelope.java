package com.vendingflow.telemetry.dto;

import com.fasterxml.jackson.databind.JsonNode;
import java.time.OffsetDateTime;

public record EventEnvelope(String eventId, String eventType, OffsetDateTime timestamp, String correlationId, String machineId, JsonNode payload) {}

package com.vendingflow.telemetry.dto;

import java.time.OffsetDateTime;

public record HealthDto(
    String machineId,
    String state,
    OffsetDateTime lastHeartbeatAt,
    String cashModuleStatus,
    String dispenserStatus,
    String doorStatus,
    Integer networkStrength) {}

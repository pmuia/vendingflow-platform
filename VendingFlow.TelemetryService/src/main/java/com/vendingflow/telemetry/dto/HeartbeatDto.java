package com.vendingflow.telemetry.dto;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

public record HeartbeatDto(
    String machineId,
    OffsetDateTime timestamp,
    BigDecimal temperature,
    Integer networkStrength,
    String cashModuleStatus,
    String dispenserStatus,
    String doorStatus,
    String softwareVersion) {}

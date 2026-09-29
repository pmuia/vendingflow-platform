package com.vendingflow.telemetry.domain;

import jakarta.persistence.Entity;
import jakarta.persistence.Id;
import java.math.BigDecimal;
import java.time.OffsetDateTime;
import java.util.UUID;

@Entity(name = "machine_telemetry")
public class MachineTelemetry {
  @Id public UUID id;
  public String machineId;
  public OffsetDateTime recordedAt;
  public BigDecimal temperature;
  public Integer networkStrength;
  public String cashModuleStatus;
  public String dispenserStatus;
  public String doorStatus;
  public String softwareVersion;
}

package com.vendingflow.telemetry.domain.model;

import java.math.BigDecimal;
import java.time.OffsetDateTime;

import com.vendingflow.telemetry.application.dto.HeartbeatDto;
import com.vendingflow.telemetry.domain.enums.MachineHealthState;
import com.vendingflow.telemetry.domain.enums.TelemetryComponentStatus;

import jakarta.persistence.Column;
import jakarta.persistence.Entity;
import jakarta.persistence.EnumType;
import jakarta.persistence.Enumerated;
import jakarta.persistence.Id;
import jakarta.persistence.Table;

@Entity
@Table(name = "machine_telemetry")
public class MachineTelemetry extends AuditableEntity {

	@Id
	@TunuGeneratedId
	@Column(name = "machine_telemetry_id")
	private Long machineTelemetryId;

	@Column(name = "machine_id", nullable = false, length = 32)
	private String machineId;

	@Column(name = "recorded_at", nullable = false)
	private OffsetDateTime recordedAt;

	@Column(name = "temperature", nullable = false, precision = 5, scale = 2)
	private BigDecimal temperature;

	@Column(name = "network_strength", nullable = false)
	private Integer networkStrength;

	@Enumerated(EnumType.STRING)
	@Column(name = "cash_module_status", nullable = false, length = 32)
	private TelemetryComponentStatus cashModuleStatus;

	@Enumerated(EnumType.STRING)
	@Column(name = "dispenser_status", nullable = false, length = 32)
	private TelemetryComponentStatus dispenserStatus;

	@Enumerated(EnumType.STRING)
	@Column(name = "door_status", nullable = false, length = 32)
	private TelemetryComponentStatus doorStatus;

	@Enumerated(EnumType.STRING)
	@Column(name = "health_state", nullable = false, length = 32)
	private MachineHealthState healthState;

	@Column(name = "software_version", nullable = false, length = 32)
	private String softwareVersion;

	protected MachineTelemetry() {
	}

	public MachineTelemetry(String machineId, OffsetDateTime recordedAt, BigDecimal temperature, Integer networkStrength,
			TelemetryComponentStatus cashModuleStatus, TelemetryComponentStatus dispenserStatus,
			TelemetryComponentStatus doorStatus, String softwareVersion) {
		validate(machineId, recordedAt, temperature, networkStrength, cashModuleStatus, dispenserStatus, doorStatus,
				softwareVersion);
		this.machineId = machineId;
		this.recordedAt = recordedAt;
		this.temperature = temperature;
		this.networkStrength = networkStrength;
		this.cashModuleStatus = cashModuleStatus;
		this.dispenserStatus = dispenserStatus;
		this.doorStatus = doorStatus;
		this.softwareVersion = softwareVersion;
		this.healthState = resolveHealthState(dispenserStatus, doorStatus, networkStrength);
	}

	public static MachineTelemetry fromHeartbeat(HeartbeatDto heartbeat) {
		return new MachineTelemetry(heartbeat.machineId(),
				heartbeat.timestamp() == null ? OffsetDateTime.now() : heartbeat.timestamp(),
				heartbeat.temperature(),
				heartbeat.networkStrength(),
				componentStatus(heartbeat.cashModuleStatus()),
				componentStatus(heartbeat.dispenserStatus()),
				componentStatus(heartbeat.doorStatus()),
				heartbeat.softwareVersion());
	}

	private static MachineHealthState resolveHealthState(TelemetryComponentStatus dispenserStatus,
			TelemetryComponentStatus doorStatus, Integer networkStrength) {
		if (dispenserStatus == TelemetryComponentStatus.ERROR || doorStatus == TelemetryComponentStatus.ERROR) {
			return MachineHealthState.DEGRADED;
		}
		if (networkStrength != null && networkStrength < 30) {
			return MachineHealthState.DEGRADED;
		}
		return MachineHealthState.ONLINE;
	}

	private static TelemetryComponentStatus componentStatus(String value) {
		if (value == null || value.isBlank()) {
			return TelemetryComponentStatus.UNKNOWN;
		}
		return TelemetryComponentStatus.valueOf(value.toUpperCase());
	}

	private static void validate(String machineId, OffsetDateTime recordedAt, BigDecimal temperature,
			Integer networkStrength, TelemetryComponentStatus cashModuleStatus, TelemetryComponentStatus dispenserStatus,
			TelemetryComponentStatus doorStatus, String softwareVersion) {
		if (machineId == null || machineId.isBlank()) {
			throw new IllegalArgumentException("Machine id is required");
		}
		if (recordedAt == null) {
			throw new IllegalArgumentException("Recorded at is required");
		}
		if (temperature == null) {
			throw new IllegalArgumentException("Temperature is required");
		}
		if (networkStrength == null) {
			throw new IllegalArgumentException("Network strength is required");
		}
		if (cashModuleStatus == null || dispenserStatus == null || doorStatus == null) {
			throw new IllegalArgumentException("Component statuses are required");
		}
		if (softwareVersion == null || softwareVersion.isBlank()) {
			throw new IllegalArgumentException("Software version is required");
		}
	}

	public Long getMachineTelemetryId() {
		return machineTelemetryId;
	}

	public String getMachineId() {
		return machineId;
	}

	public OffsetDateTime getRecordedAt() {
		return recordedAt;
	}

	public BigDecimal getTemperature() {
		return temperature;
	}

	public Integer getNetworkStrength() {
		return networkStrength;
	}

	public TelemetryComponentStatus getCashModuleStatus() {
		return cashModuleStatus;
	}

	public TelemetryComponentStatus getDispenserStatus() {
		return dispenserStatus;
	}

	public TelemetryComponentStatus getDoorStatus() {
		return doorStatus;
	}

	public MachineHealthState getHealthState() {
		return healthState;
	}

	public String getSoftwareVersion() {
		return softwareVersion;
	}
}

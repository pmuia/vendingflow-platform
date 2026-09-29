CREATE TABLE machine_telemetry (
  machine_telemetry_id BIGINT PRIMARY KEY,
  machine_id VARCHAR(32) NOT NULL,
  recorded_at TIMESTAMPTZ NOT NULL,
  temperature NUMERIC(5,2) NOT NULL,
  network_strength INT NOT NULL,
  cash_module_status VARCHAR(32) NOT NULL,
  dispenser_status VARCHAR(32) NOT NULL,
  door_status VARCHAR(32) NOT NULL,
  health_state VARCHAR(32) NOT NULL,
  created_at TIMESTAMPTZ NOT NULL,
  created_by BIGINT NULL,
  modified_at TIMESTAMPTZ NULL,
  modified_by BIGINT NULL,
  software_version VARCHAR(32) NOT NULL
);

CREATE INDEX ix_machine_telemetry_machine_recorded ON machine_telemetry(machine_id, recorded_at DESC);

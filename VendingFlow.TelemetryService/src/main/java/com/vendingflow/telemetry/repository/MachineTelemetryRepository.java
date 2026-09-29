package com.vendingflow.telemetry.repository;

import com.vendingflow.telemetry.domain.MachineTelemetry;
import java.util.List;
import java.util.UUID;
import org.springframework.data.jpa.repository.JpaRepository;

public interface MachineTelemetryRepository extends JpaRepository<MachineTelemetry, UUID> {
  List<MachineTelemetry> findTop50ByMachineIdOrderByRecordedAtDesc(String machineId);
}

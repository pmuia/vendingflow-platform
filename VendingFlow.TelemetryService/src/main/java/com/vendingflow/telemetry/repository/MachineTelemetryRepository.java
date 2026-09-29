package com.vendingflow.telemetry.repository;

import java.util.List;

import org.springframework.data.jpa.repository.JpaRepository;

import com.vendingflow.telemetry.domain.model.MachineTelemetry;

public interface MachineTelemetryRepository extends JpaRepository<MachineTelemetry, Long> {
  List<MachineTelemetry> findTop50ByMachineIdOrderByRecordedAtDesc(String machineId);
}

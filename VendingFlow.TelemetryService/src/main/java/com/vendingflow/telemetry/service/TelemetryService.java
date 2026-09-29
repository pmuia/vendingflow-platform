package com.vendingflow.telemetry.service;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.vendingflow.telemetry.domain.MachineTelemetry;
import com.vendingflow.telemetry.dto.EventEnvelope;
import com.vendingflow.telemetry.dto.HealthDto;
import com.vendingflow.telemetry.dto.HeartbeatDto;
import com.vendingflow.telemetry.repository.MachineTelemetryRepository;
import java.time.Duration;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Set;
import java.util.UUID;
import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

@Service
public class TelemetryService {
  private static final Logger log = LoggerFactory.getLogger(TelemetryService.class);
  private final MachineTelemetryRepository repository;
  private final RedisTemplate<String, Object> redis;
  private final ObjectMapper mapper;
  private final long degradedAfter;
  private final long offlineAfter;

  public TelemetryService(MachineTelemetryRepository repository, RedisTemplate<String, Object> redis, ObjectMapper mapper,
      @Value("${vendingflow.heartbeat.degraded-after-seconds}") long degradedAfter,
      @Value("${vendingflow.heartbeat.offline-after-seconds}") long offlineAfter) {
    this.repository = repository;
    this.redis = redis;
    this.mapper = mapper;
    this.degradedAfter = degradedAfter;
    this.offlineAfter = offlineAfter;
  }

  public void record(EventEnvelope envelope) {
    var heartbeat = mapper.convertValue(envelope.payload(), HeartbeatDto.class);
    var telemetry = new MachineTelemetry();
    telemetry.id = UUID.randomUUID();
    telemetry.machineId = heartbeat.machineId();
    telemetry.recordedAt = heartbeat.timestamp() == null ? OffsetDateTime.now() : heartbeat.timestamp();
    telemetry.temperature = heartbeat.temperature();
    telemetry.networkStrength = heartbeat.networkStrength();
    telemetry.cashModuleStatus = heartbeat.cashModuleStatus();
    telemetry.dispenserStatus = heartbeat.dispenserStatus();
    telemetry.doorStatus = heartbeat.doorStatus();
    telemetry.softwareVersion = heartbeat.softwareVersion();
    repository.save(telemetry);
    var state = "ERROR".equalsIgnoreCase(heartbeat.dispenserStatus()) ? "DEGRADED" : "ONLINE";
    redis.opsForValue().set(key(heartbeat.machineId()), new HealthDto(heartbeat.machineId(), state, telemetry.recordedAt, heartbeat.cashModuleStatus(), heartbeat.dispenserStatus(), heartbeat.doorStatus(), heartbeat.networkStrength()));
    log.info("heartbeat received machineId={} state={} correlationId={}", heartbeat.machineId(), state, envelope.correlationId());
  }

  @Scheduled(fixedDelay = 5000)
  public void evaluateHealth() {
    Set<String> keys = redis.keys("machine:*:health");
    if (keys == null) return;
    for (String key : keys) {
      Object value = redis.opsForValue().get(key);
      HealthDto current = mapper.convertValue(value, HealthDto.class);
      if (current == null || current.lastHeartbeatAt() == null) continue;
      long age = Duration.between(current.lastHeartbeatAt(), OffsetDateTime.now()).toSeconds();
      String next = age >= offlineAfter ? "OFFLINE" : age >= degradedAfter ? "DEGRADED" : current.state();
      if (!next.equals(current.state())) {
        redis.opsForValue().set(key, new HealthDto(current.machineId(), next, current.lastHeartbeatAt(), current.cashModuleStatus(), current.dispenserStatus(), current.doorStatus(), current.networkStrength()));
        log.warn("machine {} {}", current.machineId(), next.toLowerCase());
      }
    }
  }

  public List<HealthDto> machines() {
    Set<String> keys = redis.keys("machine:*:health");
    if (keys == null) return List.of();
    return keys.stream().map(k -> mapper.convertValue(redis.opsForValue().get(k), HealthDto.class)).toList();
  }

  public HealthDto machine(String machineId) {
    return mapper.convertValue(redis.opsForValue().get(key(machineId)), HealthDto.class);
  }

  public List<MachineTelemetry> history(String machineId) {
    return repository.findTop50ByMachineIdOrderByRecordedAtDesc(machineId);
  }

  private String key(String machineId) { return "machine:" + machineId + ":health"; }
}

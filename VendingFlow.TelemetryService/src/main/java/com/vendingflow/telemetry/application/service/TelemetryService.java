package com.vendingflow.telemetry.application.service;

import java.time.Duration;
import java.time.OffsetDateTime;
import java.util.List;
import java.util.Set;

import org.slf4j.Logger;
import org.slf4j.LoggerFactory;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.data.redis.core.RedisTemplate;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Service;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.vendingflow.telemetry.application.dto.EventEnvelope;
import com.vendingflow.telemetry.application.dto.HealthDto;
import com.vendingflow.telemetry.application.dto.HeartbeatDto;
import com.vendingflow.telemetry.application.dto.MachineTelemetryResponse;
import com.vendingflow.telemetry.domain.enums.MachineHealthState;
import com.vendingflow.telemetry.domain.model.MachineTelemetry;
import com.vendingflow.telemetry.repository.MachineTelemetryRepository;

@Service
public class TelemetryService {

	private static final Logger log = LoggerFactory.getLogger(TelemetryService.class);

	private final MachineTelemetryRepository repository;
	private final RedisTemplate<String, Object> redis;
	private final ObjectMapper mapper;
	private final long degradedAfter;
	private final long offlineAfter;

	public TelemetryService(MachineTelemetryRepository repository, RedisTemplate<String, Object> redis,
			ObjectMapper mapper, @Value("${vendingflow.heartbeat.degraded-after-seconds}") long degradedAfter,
			@Value("${vendingflow.heartbeat.offline-after-seconds}") long offlineAfter) {
		this.repository = repository;
		this.redis = redis;
		this.mapper = mapper;
		this.degradedAfter = degradedAfter;
		this.offlineAfter = offlineAfter;
	}

	public void record(EventEnvelope envelope) {
		var heartbeat = mapper.convertValue(envelope.payload(), HeartbeatDto.class);
		var telemetry = MachineTelemetry.fromHeartbeat(heartbeat);
		repository.save(telemetry);

		var health = new HealthDto(heartbeat.machineId(), telemetry.getHealthState(), telemetry.getRecordedAt(),
				telemetry.getCashModuleStatus(), telemetry.getDispenserStatus(), telemetry.getDoorStatus(),
				telemetry.getNetworkStrength());
		redis.opsForValue().set(key(heartbeat.machineId()), health);
		log.info("heartbeat received machineId={} state={} correlationId={}", heartbeat.machineId(),
				telemetry.getHealthState(), envelope.correlationId());
	}

	@Scheduled(fixedDelay = 5000)
	public void evaluateHealth() {
		Set<String> keys = redis.keys("machine:*:health");
		if (keys == null) {
			return;
		}
		for (String key : keys) {
			Object value = redis.opsForValue().get(key);
			HealthDto current = mapper.convertValue(value, HealthDto.class);
			if (current == null || current.lastHeartbeatAt() == null) {
				continue;
			}
			long age = Duration.between(current.lastHeartbeatAt(), OffsetDateTime.now()).toSeconds();
			MachineHealthState next = age >= offlineAfter ? MachineHealthState.OFFLINE
					: age >= degradedAfter ? MachineHealthState.DEGRADED : current.state();
			if (next != current.state()) {
				redis.opsForValue().set(key, new HealthDto(current.machineId(), next, current.lastHeartbeatAt(),
						current.cashModuleStatus(), current.dispenserStatus(), current.doorStatus(),
						current.networkStrength()));
				log.warn("machine {} {}", current.machineId(), next.name().toLowerCase());
			}
		}
	}

	public List<HealthDto> machines() {
		Set<String> keys = redis.keys("machine:*:health");
		if (keys == null) {
			return List.of();
		}
		return keys.stream().map(k -> mapper.convertValue(redis.opsForValue().get(k), HealthDto.class)).toList();
	}

	public HealthDto machine(String machineId) {
		return mapper.convertValue(redis.opsForValue().get(key(machineId)), HealthDto.class);
	}

	public List<MachineTelemetryResponse> history(String machineId) {
		return repository.findTop50ByMachineIdOrderByRecordedAtDesc(machineId).stream()
				.map(MachineTelemetryResponse::from)
				.toList();
	}

	private String key(String machineId) {
		return "machine:" + machineId + ":health";
	}
}

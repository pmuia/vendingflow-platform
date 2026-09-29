package com.vendingflow.telemetry.api.controller;

import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PathVariable;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RestController;

import com.vendingflow.telemetry.application.service.TelemetryService;

@RestController
@RequestMapping("/api/telemetry")
public class TelemetryController {

	private final TelemetryService service;

	public TelemetryController(TelemetryService service) {
		this.service = service;
	}

	@GetMapping("/machines")
	Object machines() {
		return service.machines();
	}

	@GetMapping("/machines/{machineId}")
	Object machine(@PathVariable String machineId) {
		return service.machine(machineId);
	}

	@GetMapping("/machines/{machineId}/history")
	Object history(@PathVariable String machineId) {
		return service.history(machineId);
	}
}

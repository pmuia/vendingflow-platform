package com.vendingflow.telemetry.consumer;

import com.fasterxml.jackson.databind.ObjectMapper;
import com.vendingflow.telemetry.application.dto.EventEnvelope;
import com.vendingflow.telemetry.application.service.TelemetryService;
import org.springframework.amqp.rabbit.annotation.RabbitListener;
import org.springframework.stereotype.Component;

@Component
public class TelemetryConsumer {
  private final ObjectMapper mapper;
  private final TelemetryService telemetryService;

  public TelemetryConsumer(ObjectMapper mapper, TelemetryService telemetryService) {
    this.mapper = mapper;
    this.telemetryService = telemetryService;
  }

  @RabbitListener(queues = "telemetry.machine")
  public void handle(String message) throws Exception {
    var envelope = mapper.readValue(message, EventEnvelope.class);
    if ("machine.heartbeat".equals(envelope.eventType())) {
      telemetryService.record(envelope);
    }
  }
}

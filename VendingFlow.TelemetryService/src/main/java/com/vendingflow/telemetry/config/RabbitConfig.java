package com.vendingflow.telemetry.config;

import org.springframework.amqp.core.Binding;
import org.springframework.amqp.core.BindingBuilder;
import org.springframework.amqp.core.Queue;
import org.springframework.amqp.core.TopicExchange;
import org.springframework.beans.factory.annotation.Value;
import org.springframework.context.annotation.Bean;
import org.springframework.context.annotation.Configuration;

@Configuration
public class RabbitConfig {
  @Bean TopicExchange exchange(@Value("${vendingflow.rabbit.exchange}") String exchange) { return new TopicExchange(exchange, true, false); }
  @Bean Queue telemetryQueue() { return new Queue("telemetry.machine", true); }
  @Bean Binding heartbeatBinding(Queue telemetryQueue, TopicExchange exchange) { return BindingBuilder.bind(telemetryQueue).to(exchange).with("machine.heartbeat"); }
  @Bean Binding faultBinding(Queue telemetryQueue, TopicExchange exchange) { return BindingBuilder.bind(telemetryQueue).to(exchange).with("machine.fault"); }
}

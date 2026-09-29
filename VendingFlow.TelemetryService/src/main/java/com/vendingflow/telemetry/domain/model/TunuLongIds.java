package com.vendingflow.telemetry.domain.model;

import java.net.InetAddress;
import java.net.UnknownHostException;
import java.time.Instant;

public final class TunuLongIds {

	private static final long EPOCH_MILLIS = Instant.parse("2026-01-01T00:00:00Z").toEpochMilli();
	private static final int NODE_ID_BITS = 10;
	private static final int SEQUENCE_BITS = 12;
	private static final long MAX_NODE_ID = (1L << NODE_ID_BITS) - 1;
	private static final long SEQUENCE_MASK = (1L << SEQUENCE_BITS) - 1;
	private static final long NODE_ID = resolveNodeId();

	private static long lastTimestamp = -1L;
	private static long sequence = 0L;

	private TunuLongIds() {
	}

	public static synchronized long nextId() {
		long timestamp = currentTimestamp();

		if (timestamp < lastTimestamp) {
			timestamp = lastTimestamp;
		}

		if (timestamp == lastTimestamp) {
			sequence = (sequence + 1) & SEQUENCE_MASK;
			if (sequence == 0L) {
				timestamp = waitForNextMillis(lastTimestamp);
			}
		}
		else {
			sequence = 0L;
		}

		lastTimestamp = timestamp;
		return ((timestamp - EPOCH_MILLIS) << (NODE_ID_BITS + SEQUENCE_BITS)) | (NODE_ID << SEQUENCE_BITS) | sequence;
	}

	private static long currentTimestamp() {
		long timestamp = System.currentTimeMillis();
		if (timestamp < EPOCH_MILLIS) {
			throw new IllegalStateException("System clock is before the Tunu id epoch");
		}
		return timestamp;
	}

	private static long waitForNextMillis(long timestamp) {
		long nextTimestamp = currentTimestamp();
		while (nextTimestamp <= timestamp) {
			Thread.onSpinWait();
			nextTimestamp = currentTimestamp();
		}
		return nextTimestamp;
	}

	private static long resolveNodeId() {
		String configuredNodeId = System.getProperty("tunu.node.id");
		if (configuredNodeId == null || configuredNodeId.isBlank()) {
			configuredNodeId = System.getenv("TUNU_NODE_ID");
		}
		if (configuredNodeId != null && !configuredNodeId.isBlank()) {
			long nodeId = Long.parseLong(configuredNodeId);
			if (nodeId < 0 || nodeId > MAX_NODE_ID) {
				throw new IllegalArgumentException("Tunu node id must be between 0 and " + MAX_NODE_ID);
			}
			return nodeId;
		}

		return Math.floorMod(hostname().hashCode(), MAX_NODE_ID + 1);
	}

	private static String hostname() {
		try {
			return InetAddress.getLocalHost().getHostName();
		}
		catch (UnknownHostException exception) {
			return "vendingflow-telemetry-service";
		}
	}
}

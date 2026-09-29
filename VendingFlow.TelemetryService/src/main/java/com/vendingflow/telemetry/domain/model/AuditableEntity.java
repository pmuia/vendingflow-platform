package com.vendingflow.telemetry.domain.model;

import java.time.Instant;

import jakarta.persistence.Column;
import jakarta.persistence.MappedSuperclass;
import jakarta.persistence.PrePersist;
import jakarta.persistence.PreUpdate;

@MappedSuperclass
public abstract class AuditableEntity {

	@Column(name = "created_at", nullable = false)
	private Instant createdAt;

	@Column(name = "created_by")
	private Long createdBy;

	@Column(name = "modified_at")
	private Instant modifiedAt;

	@Column(name = "modified_by")
	private Long modifiedBy;

	@PrePersist
	void onCreate() {
		this.createdAt = Instant.now();
	}

	@PreUpdate
	void onUpdate() {
		this.modifiedAt = Instant.now();
	}

	protected void markCreatedBy(Long createdBy) {
		this.createdBy = createdBy;
	}

	protected void markModifiedBy(Long modifiedBy) {
		this.modifiedBy = modifiedBy;
	}

	public Instant getCreatedAt() {
		return createdAt;
	}

	public Long getCreatedBy() {
		return createdBy;
	}

	public Instant getModifiedAt() {
		return modifiedAt;
	}

	public Long getModifiedBy() {
		return modifiedBy;
	}
}

-- Migration: Add Customers entity
-- Adds the `customers` table and links it to `field_mapping_templates`.
-- Safe to run against an existing database (uses IF NOT EXISTS / IF EXISTS guards).

BEGIN;

-- ─────────────────────────────────────────────
-- 1. Create customers table
-- ─────────────────────────────────────────────
CREATE TABLE IF NOT EXISTS customers (
    id          VARCHAR(100)  PRIMARY KEY,
    name        VARCHAR(200)  NOT NULL,
    code        VARCHAR(50)   NULL,
    contact_email VARCHAR(200) NULL,
    contact_phone VARCHAR(50)  NULL,
    is_active   BOOLEAN       NOT NULL DEFAULT TRUE,
    notes       TEXT          NULL,
    created_at  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at  TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    created_by  VARCHAR(100)  NULL
);

-- ─────────────────────────────────────────────
-- 2. Indexes for customers
-- ─────────────────────────────────────────────
CREATE INDEX IF NOT EXISTS idx_customers_name
    ON customers (name);

-- Partial unique index: non-null codes must be unique, but many rows may have NULL code
CREATE UNIQUE INDEX IF NOT EXISTS idx_customers_code
    ON customers (code)
    WHERE code IS NOT NULL;

CREATE INDEX IF NOT EXISTS idx_customers_is_active
    ON customers (is_active);

-- ─────────────────────────────────────────────
-- 3. Add customer_id column to field_mapping_templates
-- ─────────────────────────────────────────────
ALTER TABLE field_mapping_templates
    ADD COLUMN IF NOT EXISTS customer_id VARCHAR(100) NULL
        REFERENCES customers (id) ON DELETE SET NULL;

CREATE INDEX IF NOT EXISTS idx_templates_customer_id
    ON field_mapping_templates (customer_id);

-- ─────────────────────────────────────────────
-- 4. Optional seed data
--    Remove or adjust the block below if you do not
--    want any default customers pre-loaded.
-- ─────────────────────────────────────────────
 INSERT INTO customers (id, name, code, is_active, created_by, created_at, updated_at)
 VALUES
     (gen_random_uuid()::text, 'Cheema Transport',  'cheema',       TRUE, 'System', NOW(), NOW()),
     (gen_random_uuid()::text, 'Hill Brothers',     'hill-brothers', TRUE, 'System', NOW(), NOW())
 ON CONFLICT DO NOTHING;

COMMIT;

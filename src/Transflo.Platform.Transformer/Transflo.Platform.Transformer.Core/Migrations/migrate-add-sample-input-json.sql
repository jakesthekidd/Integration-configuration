-- Migration: add sample_input_json column to field_mapping_templates
-- Run once against the target database.

ALTER TABLE field_mapping_templates
    ADD COLUMN IF NOT EXISTS sample_input_json jsonb NULL;

COMMENT ON COLUMN field_mapping_templates.sample_input_json
    IS 'Sample source JSON payload used for previewing transformations and auto-suggesting source field paths.';

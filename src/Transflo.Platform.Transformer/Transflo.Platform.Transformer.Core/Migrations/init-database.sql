-- Field Mapping System Database Schema for PostgreSQL

-- Create database (run this separately if needed)
-- CREATE DATABASE fieldmapping;

-- Connect to the database
\c fieldmapping;

-- Enable UUID extension
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Drop tables if they exist (for clean re-run)
DROP TABLE IF EXISTS transformation_logs CASCADE;
DROP TABLE IF EXISTS field_mappings CASCADE;
DROP TABLE IF EXISTS lookup_tables CASCADE;
DROP TABLE IF EXISTS field_mapping_templates CASCADE;
DROP TABLE IF EXISTS tms_systems CASCADE;

-- Create TMS Systems table
CREATE TABLE tms_systems (
    id VARCHAR(100) PRIMARY KEY,
    name VARCHAR(100) UNIQUE NOT NULL,
    display_name VARCHAR(200) NOT NULL,
    description TEXT,
    version VARCHAR(50) DEFAULT '1.0',
    is_active BOOLEAN DEFAULT true,
    sample_json_schema JSONB,
    connection_config JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_by VARCHAR(100),
    metadata JSONB
);

-- Create indexes for TMS Systems
CREATE INDEX idx_tms_systems_name ON tms_systems(name);
CREATE INDEX idx_tms_systems_is_active ON tms_systems(is_active);

-- Create Field Mapping Templates table
CREATE TABLE field_mapping_templates (
    id SERIAL PRIMARY KEY,
    template_id VARCHAR(100) NOT NULL,
    version INTEGER DEFAULT 1,
    tms_system_id VARCHAR(100) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    status VARCHAR(50) DEFAULT 'Draft',
    source_schema JSONB,
    target_schema JSONB,
    created_by VARCHAR(100),
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    published_at TIMESTAMP WITH TIME ZONE,
    published_by VARCHAR(100),
    metadata JSONB,
    FOREIGN KEY (tms_system_id) REFERENCES tms_systems(id) ON DELETE CASCADE,
    UNIQUE(template_id, version)
);

-- Create indexes for Field Mapping Templates
CREATE INDEX idx_templates_template_id ON field_mapping_templates(template_id);
CREATE INDEX idx_templates_tms_system_id ON field_mapping_templates(tms_system_id);
CREATE INDEX idx_templates_status ON field_mapping_templates(status);

-- Create Field Mappings table
CREATE TABLE field_mappings (
    id VARCHAR(100) PRIMARY KEY,
    template_id VARCHAR(100) NOT NULL,
    source_path VARCHAR(500) NOT NULL,
    target_path VARCHAR(500) NOT NULL,
    transformation_type VARCHAR(50) DEFAULT 'Direct',
    transformation_config JSONB,
    execution_order INTEGER DEFAULT 0,
    is_required BOOLEAN DEFAULT false,
    default_value TEXT,
    validation_rules JSONB,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW()
);

-- Create indexes for Field Mappings
CREATE INDEX idx_field_mappings_template_id ON field_mappings(template_id);
CREATE INDEX idx_field_mappings_template_order ON field_mappings(template_id, execution_order);

-- Create Lookup Tables table
CREATE TABLE lookup_tables (
    id VARCHAR(100) PRIMARY KEY,
    tms_system_id VARCHAR(100) NOT NULL,
    field_name VARCHAR(200) NOT NULL,
    name VARCHAR(200) NOT NULL,
    description TEXT,
    mappings JSONB,
    default_value VARCHAR(500),
    is_case_sensitive BOOLEAN DEFAULT true,
    created_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    created_by VARCHAR(100),
    FOREIGN KEY (tms_system_id) REFERENCES tms_systems(id) ON DELETE CASCADE
);

-- Create indexes for Lookup Tables
CREATE INDEX idx_lookup_tables_tms_system_id ON lookup_tables(tms_system_id);
CREATE INDEX idx_lookup_tables_tms_field ON lookup_tables(tms_system_id, field_name);

-- Create Transformation Logs table
CREATE TABLE transformation_logs (
    id VARCHAR(100) PRIMARY KEY,
    template_id VARCHAR(100) NOT NULL,
    timestamp TIMESTAMP WITH TIME ZONE DEFAULT NOW(),
    status VARCHAR(50) NOT NULL,
    input_data JSONB,
    output_data JSONB,
    errors JSONB,
    execution_time_ms BIGINT,
    record_count INTEGER DEFAULT 1,
    user_id VARCHAR(100),
    source VARCHAR(200),
    expires_at TIMESTAMP WITH TIME ZONE DEFAULT (NOW() + INTERVAL '90 days')
);

-- Create indexes for Transformation Logs
CREATE INDEX idx_transformation_logs_template_id ON transformation_logs(template_id);
CREATE INDEX idx_transformation_logs_status ON transformation_logs(status);
CREATE INDEX idx_transformation_logs_timestamp ON transformation_logs(timestamp);
CREATE INDEX idx_transformation_logs_expires_at ON transformation_logs(expires_at);

-- Insert seed data
INSERT INTO tms_systems (id, name, display_name, description, version, is_active, created_by, created_at, updated_at)
VALUES
    ('tms-truckmate-001', 'TruckMate', 'TruckMate TMS', 'TruckMate Transportation Management System', '1.0', true, 'System', NOW(), NOW()),
    ('tms-mcleod-001', 'McLeod', 'McLeod Software', 'McLeod Transportation Management System', '1.0', true, 'System', NOW(), NOW());

-- Create a function to clean up expired logs
CREATE OR REPLACE FUNCTION cleanup_expired_logs()
RETURNS void AS $$
BEGIN
    DELETE FROM transformation_logs
    WHERE expires_at < NOW();
END;
$$ LANGUAGE plpgsql;

-- Create a scheduled job to run cleanup (requires pg_cron extension)
-- You can manually run: SELECT cleanup_expired_logs();

COMMIT;

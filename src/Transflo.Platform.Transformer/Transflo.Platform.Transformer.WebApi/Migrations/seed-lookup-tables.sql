-- Seed data for McLeod TMS Lookup Tables
-- These tables map McLeod-specific code values to WFAI canonical values

-- ============================================================
-- 1. ORDER STATUS CODES
--    McLeod: status field (single char code)
--    WFAI:   status string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-order-status',
    'tms-mcleod-001',
    'status',
    'McLeod Order Status Codes',
    'Maps McLeod single-character order status codes to WFAI human-readable status strings',
    '{
        "A": "Available",
        "D": "Delivered",
        "P": "In Progress",
        "Q": "Pending",
        "C": "Cancelled",
        "H": "On Hold",
        "I": "In Transit",
        "X": "Cancelled",
        "V": "Void",
        "B": "Booked",
        "E": "En Route"
    }'::jsonb,
    'Unknown',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 2. STOP TYPE CODES
--    McLeod: stop_type field
--    WFAI:   type string on stop
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-stop-type',
    'tms-mcleod-001',
    'stop_type',
    'McLeod Stop Type Codes',
    'Maps McLeod stop type codes to WFAI stop type labels',
    '{
        "PU": "Pickup",
        "SO": "Drop",
        "SP": "Split Pickup",
        "SD": "Split Drop",
        "TF": "Transfer",
        "TC": "Transfer Carrier",
        "HK": "Hook",
        "DH": "Drop Hook",
        "FL": "Fuel",
        "RS": "Rest Stop",
        "BI": "Border Inspection"
    }'::jsonb,
    'Stop',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 3. RATE TYPE CODES
--    McLeod: rate_type field
--    WFAI:   rateType string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-rate-type',
    'tms-mcleod-001',
    'rate_type',
    'McLeod Rate Type Codes',
    'Maps McLeod rate type codes to WFAI rate type labels',
    '{
        "F": "Flat",
        "M": "Per Mile",
        "L": "Per Load",
        "P": "Per Pound",
        "T": "Per Ton",
        "H": "Per Hour",
        "S": "Per Stop",
        "U": "Per Unit",
        "C": "Per Hundredweight",
        "D": "Per Day"
    }'::jsonb,
    'Flat',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 4. COLLECTION METHOD CODES
--    McLeod: collection_method field
--    WFAI:   collectionMethod string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-collection-method',
    'tms-mcleod-001',
    'collection_method',
    'McLeod Collection Method Codes',
    'Maps McLeod billing collection method codes to WFAI labels',
    '{
        "P": "Prepaid",
        "C": "Collect",
        "T": "Third Party",
        "R": "Recipient",
        "B": "Both",
        "N": "Non-Revenue"
    }'::jsonb,
    'Prepaid',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 5. ORDER TYPE CODES
--    McLeod: order_type_id field
--    WFAI:   orderType string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-order-type',
    'tms-mcleod-001',
    'order_type_id',
    'McLeod Order Type Codes',
    'Maps McLeod order type IDs to WFAI order type labels',
    '{
        "PO": "Power Only",
        "TL": "Truckload",
        "LT": "Less Than Truckload",
        "IM": "Intermodal",
        "FL": "Flatbed",
        "RE": "Reefer",
        "HZ": "Hazmat",
        "OW": "Overweight",
        "OD": "Oversize/Overwidth",
        "DD": "Double Drop",
        "SD": "Step Deck",
        "BT": "Boat Transport",
        "CV": "Conestoga Van"
    }'::jsonb,
    'Truckload',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 6. EQUIPMENT TYPE CODES
--    McLeod: equipment_type_id field
--    WFAI:   equipmentType string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-equipment-type',
    'tms-mcleod-001',
    'equipment_type_id',
    'McLeod Equipment Type Codes',
    'Maps McLeod equipment type IDs to WFAI equipment type labels',
    '{
        "V": "Van",
        "R": "Reefer",
        "F": "Flatbed",
        "SD": "Step Deck",
        "DD": "Double Drop",
        "RGN": "Removable Goose Neck",
        "LB": "Lowboy",
        "TB": "Tanker Bulk",
        "TL": "Tanker Liquid",
        "PO": "Power Only",
        "CV": "Conestoga Van",
        "HZ": "Hazmat Van",
        "IM": "Intermodal Container",
        "AC": "Auto Carrier"
    }'::jsonb,
    'Van',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 7. BROKERAGE STATUS CODES
--    McLeod: movement.brokerage_status
--    WFAI:   brokerageStatus string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-brokerage-status',
    'tms-mcleod-001',
    'brokerage_status',
    'McLeod Brokerage Status Codes',
    'Maps McLeod brokerage status codes to WFAI brokerage status labels',
    '{
        "AVAIL": "Available",
        "BOOK": "Booked",
        "PICK": "Picked Up",
        "DELVD": "Delivered",
        "VOID": "Voided",
        "CANCEL": "Cancelled",
        "INTRANS": "In Transit",
        "TENDER": "Tendered",
        "ACCEPT": "Accepted",
        "REJECT": "Rejected"
    }'::jsonb,
    'Available',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 8. DRIVER LOAD/UNLOAD ACTION CODES
--    McLeod: stop.driver_load_unload
--    WFAI:   loadAction string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-load-action',
    'tms-mcleod-001',
    'driver_load_unload',
    'McLeod Driver Load/Unload Action Codes',
    'Maps McLeod driver load/unload action codes to WFAI labels',
    '{
        "DROP": "Drop",
        "LVE": "Live",
        "HOOK": "Hook",
        "LIVE": "Live Load",
        "DH": "Drop and Hook",
        "PH": "Pre-Loaded Hook",
        "DP": "Driver Assist Partial",
        "TL": "Team Lift",
        "LF": "Lift Gate"
    }'::jsonb,
    'Live',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 9. TRANSPORTATION MODE
--    McLeod: order_mode field
--    WFAI:   mode string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-order-mode',
    'tms-mcleod-001',
    'order_mode',
    'McLeod Order Mode Codes',
    'Maps McLeod order mode codes to WFAI transportation mode labels',
    '{
        "T": "TL",
        "L": "LTL",
        "I": "Intermodal",
        "O": "Ocean",
        "A": "Air",
        "R": "Rail",
        "P": "Parcel",
        "X": "Expedited"
    }'::jsonb,
    'TL',
    true,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- 10. CONTACT TYPE CODES
--     McLeod: derived from user context (dispatcher, operations, etc.)
--     WFAI:   contactType string
-- ============================================================
INSERT INTO lookup_tables (id, tms_system_id, field_name, name, description, mappings, default_value, is_case_sensitive, created_at, updated_at, created_by)
VALUES (
    'lut-mcleod-contact-type',
    'tms-mcleod-001',
    'contact_type',
    'McLeod Contact Type Codes',
    'Maps McLeod user role/context to WFAI contact type labels',
    '{
        "dispatcher": "Dispatcher",
        "operations": "Operations",
        "billing": "Billing",
        "driver": "Driver",
        "broker": "Broker",
        "agent": "Agent",
        "manager": "Manager",
        "collections": "Collections"
    }'::jsonb,
    'Operations',
    false,
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (id) DO NOTHING;

COMMIT;

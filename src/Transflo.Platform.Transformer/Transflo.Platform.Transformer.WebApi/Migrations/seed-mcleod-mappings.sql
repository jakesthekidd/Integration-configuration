-- Seed data for McLeod TMS to WFAI transformation
-- This script creates a complete mapping template with diverse transformation types
-- and validation rules for transforming McLeod JSON to WFAI format
--
-- Source:  McLeod order JSON (stops[], movement[], customer, carrier fields)
-- Target:  WFAI canonical format (stops[].location, stops[].freight, contacts[], etc.)

-- ============================================================
-- Step 1: Ensure McLeod TMS system exists
-- ============================================================
INSERT INTO tms_systems (id, name, display_name, description, version, is_active, created_at, updated_at)
VALUES (
    'tms-mcleod-001',
    'McLeod',
    'McLeod TMS',
    'McLeod Transportation Management System',
    '1.0',
    true,
    NOW(),
    NOW()
)
ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 2: Create the mapping template
-- ============================================================
INSERT INTO field_mapping_templates (
    template_id, name, description, tms_system_id, version,
    status, created_at, updated_at, created_by
)
VALUES (
    'tmpl-mcleod-wfai-001',
    'McLeod to WFAI Transformation',
    'Complete field mapping template for transforming McLeod order data to WFAI format. Handles status lookups, date format conversion, nested stop arrays, and carrier/customer contact mapping.',
    'tms-mcleod-001',
    1,
    'Published',
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (template_id, version) DO NOTHING;

-- ============================================================
-- Step 3: Root-level order fields
--   Transformation types: Direct, Lookup, DateFormat, Constant
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES
-- Direct: order ID copied verbatim
(
    'fm-mcleod-001',
    'tmpl-mcleod-wfai-001',
    'id',
    'externalId',
    'Direct',
    NULL,
    10,
    true,
    '[{"Type":"Required","ErrorMessage":"Order ID is required"},{"Type":"Length","MinLength":1,"MaxLength":100,"ErrorMessage":"Order ID must be between 1 and 100 characters"}]',
    NOW(), NOW()
),

-- Lookup: status code D/A/P/Q/C → human-readable string
(
    'fm-mcleod-002',
    'tmpl-mcleod-wfai-001',
    'status',
    'status',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-order-status"}',
    20,
    true,
    '[{"Type":"Required","ErrorMessage":"Status is required"},{"Type":"Enum","AllowedValues":["Available","Delivered","In Progress","Pending","Cancelled","On Hold","In Transit","Void","Booked","En Route"],"ErrorMessage":"Status must be a valid WFAI status value"}]',
    NOW(), NOW()
),

-- DateFormat: McLeod yyyyMMddHHmmssK → ISO 8601 UTC
(
    'fm-mcleod-003',
    'tmpl-mcleod-wfai-001',
    'pickup_date',
    'pickupDate',
    'DateFormat',
    '{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o","OutputTimezone":"UTC"}',
    30,
    true,
    '[{"Type":"Required","ErrorMessage":"Pickup date is required"},{"Type":"Date","ErrorMessage":"Pickup date must be a valid date"}]',
    NOW(), NOW()
),

-- DateFormat: delivery date
(
    'fm-mcleod-004',
    'tmpl-mcleod-wfai-001',
    'delivery_date',
    'deliveryDate',
    'DateFormat',
    '{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o","OutputTimezone":"UTC"}',
    40,
    false,
    '[{"Type":"Date","ErrorMessage":"Delivery date must be a valid date"}]',
    NOW(), NOW()
),

-- Lookup: order mode T/L/I → TL/LTL/Intermodal
(
    'fm-mcleod-005',
    'tmpl-mcleod-wfai-001',
    'order_mode',
    'mode',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-order-mode"}',
    50,
    false,
    '[{"Type":"Enum","AllowedValues":["TL","LTL","Intermodal","Ocean","Air","Rail","Parcel","Expedited"],"ErrorMessage":"Mode must be a valid transportation mode"}]',
    NOW(), NOW()
),

-- Direct: total charges
(
    'fm-mcleod-006',
    'tmpl-mcleod-wfai-001',
    'total_charges',
    'totalAmount',
    'Direct',
    NULL,
    60,
    false,
    '[{"Type":"Range","MinValue":0,"ErrorMessage":"Total amount cannot be negative"}]',
    NOW(), NOW()
),

-- Lookup: rate type F/M/L → Flat/Per Mile/Per Load
(
    'fm-mcleod-007',
    'tmpl-mcleod-wfai-001',
    'rate_type',
    'rateType',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-rate-type"}',
    70,
    false,
    NULL,
    NOW(), NOW()
),

-- Lookup: collection method P/C/T → Prepaid/Collect/Third Party
(
    'fm-mcleod-008',
    'tmpl-mcleod-wfai-001',
    'collection_method',
    'collectionMethod',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-collection-method"}',
    80,
    false,
    NULL,
    NOW(), NOW()
),

-- Lookup: order type TL/LT/IM → Truckload/Less Than Truckload/Intermodal
(
    'fm-mcleod-009',
    'tmpl-mcleod-wfai-001',
    'order_type_id',
    'orderType',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-order-type"}',
    90,
    false,
    NULL,
    NOW(), NOW()
),

-- Lookup: equipment type V/R/F → Van/Reefer/Flatbed
(
    'fm-mcleod-010',
    'tmpl-mcleod-wfai-001',
    'equipment_type_id',
    'equipmentType',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-equipment-type"}',
    100,
    false,
    NULL,
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 4: Movement / reference number fields
--   Transformation types: Direct, ArrayMap
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- Direct: movement ID copied to primary reference
(
    'fm-mcleod-020',
    'tmpl-mcleod-wfai-001',
    'movement[0].movement_id',
    'movementId',
    'Direct',
    NULL,
    110,
    false,
    '[{"Type":"Length","MaxLength":50,"ErrorMessage":"Movement ID cannot exceed 50 characters"}]',
    NOW(), NOW()
),

-- ArrayMap: wrap single movement_id value into movementNumbers array element
(
    'fm-mcleod-021',
    'tmpl-mcleod-wfai-001',
    'movement[*].movement_id',
    'movementNumbers',
    'ArrayMap',
    '{"ItemPath":"movement_id","OutputType":"string"}',
    120,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: pro number / bill of lading
(
    'fm-mcleod-022',
    'tmpl-mcleod-wfai-001',
    'pro_number',
    'proNumber',
    'Direct',
    NULL,
    130,
    false,
    '[{"Type":"Format","Pattern":"^[A-Z0-9\\-]{1,30}$","ErrorMessage":"Pro number must contain only uppercase letters, numbers and hyphens (max 30 chars)"}]',
    NOW(), NOW()
),

-- Direct: purchase order number
(
    'fm-mcleod-023',
    'tmpl-mcleod-wfai-001',
    'po_number',
    'purchaseOrderNumber',
    'Direct',
    NULL,
    140,
    false,
    '[{"Type":"Length","MaxLength":50,"ErrorMessage":"PO number cannot exceed 50 characters"}]',
    NOW(), NOW()
),

-- Lookup: brokerage status
(
    'fm-mcleod-024',
    'tmpl-mcleod-wfai-001',
    'movement[0].brokerage_status',
    'brokerageStatus',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-brokerage-status"}',
    150,
    false,
    NULL,
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 5: Customer / shipper fields
--   Transformation types: Direct, Concat
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- Direct: customer external ID
(
    'fm-mcleod-030',
    'tmpl-mcleod-wfai-001',
    'customer.id',
    'customer.externalId',
    'Direct',
    NULL,
    200,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: customer name
(
    'fm-mcleod-031',
    'tmpl-mcleod-wfai-001',
    'customer.name',
    'customer.name',
    'Direct',
    NULL,
    210,
    true,
    '[{"Type":"Required","ErrorMessage":"Customer name is required"},{"Type":"Length","MaxLength":200,"ErrorMessage":"Customer name cannot exceed 200 characters"}]',
    NOW(), NOW()
),

-- Direct: customer phone
(
    'fm-mcleod-032',
    'tmpl-mcleod-wfai-001',
    'customer.phone',
    'customer.phone',
    'Direct',
    NULL,
    220,
    false,
    '[{"Type":"Phone","ErrorMessage":"Customer phone must be a valid phone number"}]',
    NOW(), NOW()
),

-- Direct: customer email
(
    'fm-mcleod-033',
    'tmpl-mcleod-wfai-001',
    'customer.email',
    'customer.email',
    'Direct',
    NULL,
    230,
    false,
    '[{"Type":"Email","ErrorMessage":"Customer email must be a valid email address"}]',
    NOW(), NOW()
),

-- Concat: full address from address fields
(
    'fm-mcleod-034',
    'tmpl-mcleod-wfai-001',
    'customer.address1',
    'customer.address',
    'Concat',
    '{"Fields":["customer.address1","customer.address2"],"Separator":", ","SkipEmpty":true}',
    240,
    false,
    '[{"Type":"Length","MaxLength":500,"ErrorMessage":"Customer address cannot exceed 500 characters"}]',
    NOW(), NOW()
),

-- Direct: city
(
    'fm-mcleod-035',
    'tmpl-mcleod-wfai-001',
    'customer.city',
    'customer.city',
    'Direct',
    NULL,
    250,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: state
(
    'fm-mcleod-036',
    'tmpl-mcleod-wfai-001',
    'customer.state',
    'customer.state',
    'Direct',
    NULL,
    260,
    false,
    '[{"Type":"Length","MinLength":2,"MaxLength":2,"ErrorMessage":"State must be a 2-character code"}]',
    NOW(), NOW()
),

-- Direct: zip code
(
    'fm-mcleod-037',
    'tmpl-mcleod-wfai-001',
    'customer.zip',
    'customer.zip',
    'Direct',
    NULL,
    270,
    false,
    '[{"Type":"Format","Pattern":"^\\d{5}(-\\d{4})?$","ErrorMessage":"Zip code must be in format 12345 or 12345-6789"}]',
    NOW(), NOW()
),

-- Constant: country is always US for domestic McLeod orders
(
    'fm-mcleod-038',
    'tmpl-mcleod-wfai-001',
    NULL,
    'customer.country',
    'Constant',
    '{"Value":"US"}',
    280,
    false,
    NULL,
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 6: Carrier fields
--   Transformation types: Direct, Concat, Constant
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- Direct: carrier ID (SCAC)
(
    'fm-mcleod-040',
    'tmpl-mcleod-wfai-001',
    'movement[0].carrier_id',
    'carrier.scac',
    'Direct',
    NULL,
    300,
    false,
    '[{"Type":"Length","MinLength":2,"MaxLength":4,"ErrorMessage":"SCAC code must be 2-4 characters"},{"Type":"Format","Pattern":"^[A-Z]{2,4}$","ErrorMessage":"SCAC code must contain only uppercase letters"}]',
    NOW(), NOW()
),

-- Direct: carrier name
(
    'fm-mcleod-041',
    'tmpl-mcleod-wfai-001',
    'movement[0].carrier_name',
    'carrier.name',
    'Direct',
    NULL,
    310,
    false,
    '[{"Type":"Length","MaxLength":200,"ErrorMessage":"Carrier name cannot exceed 200 characters"}]',
    NOW(), NOW()
),

-- Direct: carrier phone
(
    'fm-mcleod-042',
    'tmpl-mcleod-wfai-001',
    'movement[0].carrier_phone',
    'carrier.phone',
    'Direct',
    NULL,
    320,
    false,
    '[{"Type":"Phone","ErrorMessage":"Carrier phone must be a valid phone number"}]',
    NOW(), NOW()
),

-- Direct: driver name
(
    'fm-mcleod-043',
    'tmpl-mcleod-wfai-001',
    'movement[0].driver_id',
    'carrier.driverId',
    'Direct',
    NULL,
    330,
    false,
    NULL,
    NOW(), NOW()
),

-- Concat: driver full name from first + last
(
    'fm-mcleod-044',
    'tmpl-mcleod-wfai-001',
    'movement[0].driver_first_name',
    'carrier.driverName',
    'Concat',
    '{"Fields":["movement[0].driver_first_name","movement[0].driver_last_name"],"Separator":" ","SkipEmpty":true}',
    340,
    false,
    '[{"Type":"Length","MaxLength":150,"ErrorMessage":"Driver name cannot exceed 150 characters"}]',
    NOW(), NOW()
),

-- Direct: truck number
(
    'fm-mcleod-045',
    'tmpl-mcleod-wfai-001',
    'movement[0].tractor_id',
    'carrier.truckNumber',
    'Direct',
    NULL,
    350,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: trailer number
(
    'fm-mcleod-046',
    'tmpl-mcleod-wfai-001',
    'movement[0].trailer_id',
    'carrier.trailerNumber',
    'Direct',
    NULL,
    360,
    false,
    NULL,
    NOW(), NOW()
),

-- Constant: carrier country defaults to US
(
    'fm-mcleod-047',
    'tmpl-mcleod-wfai-001',
    NULL,
    'carrier.country',
    'Constant',
    '{"Value":"US"}',
    370,
    false,
    NULL,
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 7: Stops array – pickup stop (stops[0])
--   Transformation types: Direct, Lookup, DateFormat, Constant
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- Lookup: stop type PU → Pickup
(
    'fm-mcleod-050',
    'tmpl-mcleod-wfai-001',
    'stops[0].stop_type',
    'stops[0].type',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-stop-type"}',
    400,
    true,
    '[{"Type":"Required","ErrorMessage":"Stop type is required"},{"Type":"Enum","AllowedValues":["Pickup","Drop","Split Pickup","Split Drop","Transfer","Transfer Carrier","Hook","Drop Hook","Fuel","Rest Stop","Border Inspection"],"ErrorMessage":"Stop type must be a valid WFAI stop type"}]',
    NOW(), NOW()
),

-- Direct: stop sequence number
(
    'fm-mcleod-051',
    'tmpl-mcleod-wfai-001',
    'stops[0].stop_num',
    'stops[0].sequence',
    'Direct',
    NULL,
    410,
    true,
    '[{"Type":"Required","ErrorMessage":"Stop sequence is required"},{"Type":"Range","MinValue":1,"ErrorMessage":"Stop sequence must be at least 1"}]',
    NOW(), NOW()
),

-- Direct: facility / location name
(
    'fm-mcleod-052',
    'tmpl-mcleod-wfai-001',
    'stops[0].company_id',
    'stops[0].externalId',
    'Direct',
    NULL,
    420,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: location name
(
    'fm-mcleod-053',
    'tmpl-mcleod-wfai-001',
    'stops[0].company_name',
    'stops[0].location.name',
    'Direct',
    NULL,
    430,
    true,
    '[{"Type":"Required","ErrorMessage":"Stop location name is required"},{"Type":"Length","MaxLength":200,"ErrorMessage":"Location name cannot exceed 200 characters"}]',
    NOW(), NOW()
),

-- Direct: address line 1
(
    'fm-mcleod-054',
    'tmpl-mcleod-wfai-001',
    'stops[0].address',
    'stops[0].location.address1',
    'Direct',
    NULL,
    440,
    true,
    '[{"Type":"Required","ErrorMessage":"Stop address is required"}]',
    NOW(), NOW()
),

-- Direct: city
(
    'fm-mcleod-055',
    'tmpl-mcleod-wfai-001',
    'stops[0].city',
    'stops[0].location.city',
    'Direct',
    NULL,
    450,
    true,
    '[{"Type":"Required","ErrorMessage":"Stop city is required"}]',
    NOW(), NOW()
),

-- Direct: state
(
    'fm-mcleod-056',
    'tmpl-mcleod-wfai-001',
    'stops[0].state',
    'stops[0].location.state',
    'Direct',
    NULL,
    460,
    true,
    '[{"Type":"Required","ErrorMessage":"Stop state is required"},{"Type":"Length","MinLength":2,"MaxLength":2,"ErrorMessage":"State must be a 2-character code"}]',
    NOW(), NOW()
),

-- Direct: zip
(
    'fm-mcleod-057',
    'tmpl-mcleod-wfai-001',
    'stops[0].zip',
    'stops[0].location.zip',
    'Direct',
    NULL,
    470,
    false,
    '[{"Type":"Format","Pattern":"^\\d{5}(-\\d{4})?$","ErrorMessage":"Zip code must be in format 12345 or 12345-6789"}]',
    NOW(), NOW()
),

-- Constant: country
(
    'fm-mcleod-058',
    'tmpl-mcleod-wfai-001',
    NULL,
    'stops[0].location.country',
    'Constant',
    '{"Value":"US"}',
    480,
    false,
    NULL,
    NOW(), NOW()
),

-- DateFormat: scheduled arrival at pickup
(
    'fm-mcleod-059',
    'tmpl-mcleod-wfai-001',
    'stops[0].sched_arrive_early',
    'stops[0].scheduledArrival',
    'DateFormat',
    '{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o","OutputTimezone":"UTC"}',
    490,
    false,
    '[{"Type":"Date","ErrorMessage":"Scheduled arrival must be a valid date"}]',
    NOW(), NOW()
),

-- DateFormat: actual arrival
(
    'fm-mcleod-060',
    'tmpl-mcleod-wfai-001',
    'stops[0].actual_arrival',
    'stops[0].actualArrival',
    'DateFormat',
    '{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o","OutputTimezone":"UTC"}',
    500,
    false,
    '[{"Type":"Date","ErrorMessage":"Actual arrival must be a valid date"}]',
    NOW(), NOW()
),

-- DateFormat: actual departure
(
    'fm-mcleod-061',
    'tmpl-mcleod-wfai-001',
    'stops[0].actual_departure',
    'stops[0].actualDeparture',
    'DateFormat',
    '{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o","OutputTimezone":"UTC"}',
    510,
    false,
    '[{"Type":"Date","ErrorMessage":"Actual departure must be a valid date"}]',
    NOW(), NOW()
),

-- Lookup: driver load/unload action at pickup stop
(
    'fm-mcleod-062',
    'tmpl-mcleod-wfai-001',
    'stops[0].driver_load_unload',
    'stops[0].loadAction',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-load-action"}',
    520,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: stop phone
(
    'fm-mcleod-063',
    'tmpl-mcleod-wfai-001',
    'stops[0].phone',
    'stops[0].phone',
    'Direct',
    NULL,
    530,
    false,
    '[{"Type":"Phone","ErrorMessage":"Stop phone must be a valid phone number"}]',
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 8: Stops array – pickup freight details (stops[0].freight)
--   Transformation types: Direct, Constant
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- Direct: commodity description
(
    'fm-mcleod-070',
    'tmpl-mcleod-wfai-001',
    'stops[0].commodity',
    'stops[0].freight.description',
    'Direct',
    NULL,
    600,
    false,
    '[{"Type":"Length","MaxLength":500,"ErrorMessage":"Commodity description cannot exceed 500 characters"}]',
    NOW(), NOW()
),

-- Direct: piece count
(
    'fm-mcleod-071',
    'tmpl-mcleod-wfai-001',
    'stops[0].pieces',
    'stops[0].freight.pieces',
    'Direct',
    NULL,
    610,
    false,
    '[{"Type":"Range","MinValue":0,"ErrorMessage":"Pieces cannot be negative"}]',
    NOW(), NOW()
),

-- Direct: weight
(
    'fm-mcleod-072',
    'tmpl-mcleod-wfai-001',
    'stops[0].weight',
    'stops[0].freight.weight',
    'Direct',
    NULL,
    620,
    false,
    '[{"Type":"Range","MinValue":0,"ErrorMessage":"Weight cannot be negative"}]',
    NOW(), NOW()
),

-- Direct: weight unit of measure
(
    'fm-mcleod-073',
    'tmpl-mcleod-wfai-001',
    'stops[0].weight_uom',
    'stops[0].freight.weightUnit',
    'Direct',
    NULL,
    630,
    false,
    '[{"Type":"Enum","AllowedValues":["LBS","KGS","TON"],"ErrorMessage":"Weight unit must be LBS, KGS, or TON"}]',
    NOW(), NOW()
),

-- Direct: volume / cubic feet
(
    'fm-mcleod-074',
    'tmpl-mcleod-wfai-001',
    'stops[0].volume',
    'stops[0].freight.volume',
    'Direct',
    NULL,
    640,
    false,
    '[{"Type":"Range","MinValue":0,"ErrorMessage":"Volume cannot be negative"}]',
    NOW(), NOW()
),

-- Constant: pallets in always starts at 0 (calculated downstream)
(
    'fm-mcleod-075',
    'tmpl-mcleod-wfai-001',
    NULL,
    'stops[0].freight.palletsIn',
    'Constant',
    '{"Value":0}',
    650,
    false,
    NULL,
    NOW(), NOW()
),

-- Constant: pallets out always starts at 0
(
    'fm-mcleod-076',
    'tmpl-mcleod-wfai-001',
    NULL,
    'stops[0].freight.palletsOut',
    'Constant',
    '{"Value":0}',
    660,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: hazmat flag
(
    'fm-mcleod-077',
    'tmpl-mcleod-wfai-001',
    'stops[0].hazmat_flag',
    'stops[0].freight.isHazmat',
    'Direct',
    NULL,
    670,
    false,
    NULL,
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 9: Stops array – delivery stop (stops[1])
--   Same transformation pattern as pickup, mapped to stops[1]
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- Lookup: stop type SO → Drop
(
    'fm-mcleod-080',
    'tmpl-mcleod-wfai-001',
    'stops[1].stop_type',
    'stops[1].type',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-stop-type"}',
    700,
    false,
    '[{"Type":"Enum","AllowedValues":["Pickup","Drop","Split Pickup","Split Drop","Transfer","Transfer Carrier","Hook","Drop Hook","Fuel","Rest Stop","Border Inspection"],"ErrorMessage":"Stop type must be a valid WFAI stop type"}]',
    NOW(), NOW()
),

-- Direct: stop sequence
(
    'fm-mcleod-081',
    'tmpl-mcleod-wfai-001',
    'stops[1].stop_num',
    'stops[1].sequence',
    'Direct',
    NULL,
    710,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: delivery location name (consignee)
(
    'fm-mcleod-082',
    'tmpl-mcleod-wfai-001',
    'stops[1].company_name',
    'stops[1].location.name',
    'Direct',
    NULL,
    720,
    false,
    '[{"Type":"Length","MaxLength":200,"ErrorMessage":"Consignee name cannot exceed 200 characters"}]',
    NOW(), NOW()
),

-- Direct: delivery address
(
    'fm-mcleod-083',
    'tmpl-mcleod-wfai-001',
    'stops[1].address',
    'stops[1].location.address1',
    'Direct',
    NULL,
    730,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: delivery city
(
    'fm-mcleod-084',
    'tmpl-mcleod-wfai-001',
    'stops[1].city',
    'stops[1].location.city',
    'Direct',
    NULL,
    740,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: delivery state
(
    'fm-mcleod-085',
    'tmpl-mcleod-wfai-001',
    'stops[1].state',
    'stops[1].location.state',
    'Direct',
    NULL,
    750,
    false,
    '[{"Type":"Length","MinLength":2,"MaxLength":2,"ErrorMessage":"State must be a 2-character code"}]',
    NOW(), NOW()
),

-- Direct: delivery zip
(
    'fm-mcleod-086',
    'tmpl-mcleod-wfai-001',
    'stops[1].zip',
    'stops[1].location.zip',
    'Direct',
    NULL,
    760,
    false,
    '[{"Type":"Format","Pattern":"^\\d{5}(-\\d{4})?$","ErrorMessage":"Zip code must be in format 12345 or 12345-6789"}]',
    NOW(), NOW()
),

-- Constant: country
(
    'fm-mcleod-087',
    'tmpl-mcleod-wfai-001',
    NULL,
    'stops[1].location.country',
    'Constant',
    '{"Value":"US"}',
    770,
    false,
    NULL,
    NOW(), NOW()
),

-- DateFormat: scheduled arrival at delivery
(
    'fm-mcleod-088',
    'tmpl-mcleod-wfai-001',
    'stops[1].sched_arrive_early',
    'stops[1].scheduledArrival',
    'DateFormat',
    '{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o","OutputTimezone":"UTC"}',
    780,
    false,
    '[{"Type":"Date","ErrorMessage":"Scheduled arrival must be a valid date"}]',
    NOW(), NOW()
),

-- DateFormat: actual arrival at delivery
(
    'fm-mcleod-089',
    'tmpl-mcleod-wfai-001',
    'stops[1].actual_arrival',
    'stops[1].actualArrival',
    'DateFormat',
    '{"DateInputFormat":"yyyyMMddHHmmsszzz","DateOutputFormat":"o","OutputTimezone":"UTC"}',
    790,
    false,
    '[{"Type":"Date","ErrorMessage":"Actual arrival must be a valid date"}]',
    NOW(), NOW()
),

-- Lookup: load action at delivery stop
(
    'fm-mcleod-090',
    'tmpl-mcleod-wfai-001',
    'stops[1].driver_load_unload',
    'stops[1].loadAction',
    'Lookup',
    '{"LookupTableId":"lut-mcleod-load-action"}',
    800,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: delivery freight pieces
(
    'fm-mcleod-091',
    'tmpl-mcleod-wfai-001',
    'stops[1].pieces',
    'stops[1].freight.pieces',
    'Direct',
    NULL,
    810,
    false,
    '[{"Type":"Range","MinValue":0,"ErrorMessage":"Pieces cannot be negative"}]',
    NOW(), NOW()
),

-- Direct: delivery freight weight
(
    'fm-mcleod-092',
    'tmpl-mcleod-wfai-001',
    'stops[1].weight',
    'stops[1].freight.weight',
    'Direct',
    NULL,
    820,
    false,
    '[{"Type":"Range","MinValue":0,"ErrorMessage":"Weight cannot be negative"}]',
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 10: Contacts array
--   Transformation types: Direct, Concat, Lookup, Constant
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- Direct: dispatcher ID
(
    'fm-mcleod-100',
    'tmpl-mcleod-wfai-001',
    'dispatcher_id',
    'contacts[0].externalId',
    'Direct',
    NULL,
    900,
    false,
    NULL,
    NOW(), NOW()
),

-- Constant: dispatcher contact type
(
    'fm-mcleod-101',
    'tmpl-mcleod-wfai-001',
    NULL,
    'contacts[0].contactType',
    'Constant',
    '{"Value":"Dispatcher"}',
    910,
    false,
    NULL,
    NOW(), NOW()
),

-- Concat: dispatcher full name
(
    'fm-mcleod-102',
    'tmpl-mcleod-wfai-001',
    'dispatcher_first_name',
    'contacts[0].name',
    'Concat',
    '{"Fields":["dispatcher_first_name","dispatcher_last_name"],"Separator":" ","SkipEmpty":true}',
    920,
    false,
    '[{"Type":"Length","MaxLength":150,"ErrorMessage":"Contact name cannot exceed 150 characters"}]',
    NOW(), NOW()
),

-- Direct: dispatcher email
(
    'fm-mcleod-103',
    'tmpl-mcleod-wfai-001',
    'dispatcher_email',
    'contacts[0].email',
    'Direct',
    NULL,
    930,
    false,
    '[{"Type":"Email","ErrorMessage":"Dispatcher email must be a valid email address"}]',
    NOW(), NOW()
),

-- Direct: dispatcher phone
(
    'fm-mcleod-104',
    'tmpl-mcleod-wfai-001',
    'dispatcher_phone',
    'contacts[0].phone',
    'Direct',
    NULL,
    940,
    false,
    '[{"Type":"Phone","ErrorMessage":"Dispatcher phone must be a valid phone number"}]',
    NOW(), NOW()
),

-- Direct: operations/billing contact
(
    'fm-mcleod-105',
    'tmpl-mcleod-wfai-001',
    'ops_contact_id',
    'contacts[1].externalId',
    'Direct',
    NULL,
    950,
    false,
    NULL,
    NOW(), NOW()
),

-- Constant: operations contact type
(
    'fm-mcleod-106',
    'tmpl-mcleod-wfai-001',
    NULL,
    'contacts[1].contactType',
    'Constant',
    '{"Value":"Operations"}',
    960,
    false,
    NULL,
    NOW(), NOW()
),

-- Direct: operations contact name
(
    'fm-mcleod-107',
    'tmpl-mcleod-wfai-001',
    'ops_contact_name',
    'contacts[1].name',
    'Direct',
    NULL,
    970,
    false,
    '[{"Type":"Length","MaxLength":150,"ErrorMessage":"Contact name cannot exceed 150 characters"}]',
    NOW(), NOW()
),

-- Direct: operations email
(
    'fm-mcleod-108',
    'tmpl-mcleod-wfai-001',
    'ops_email',
    'contacts[1].email',
    'Direct',
    NULL,
    980,
    false,
    '[{"Type":"Email","ErrorMessage":"Operations email must be a valid email address"}]',
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

-- ============================================================
-- Step 11: ArrayFlatten example – consolidate all stop notes
--   Transformation type: ArrayFlatten
-- ============================================================
INSERT INTO field_mappings (
    id, template_id, source_path, target_path, transformation_type,
    transformation_config, execution_order, is_required,
    validation_rules, created_at, updated_at
)
VALUES

-- ArrayFlatten: collect note fields from all stops into a single notes array
(
    'fm-mcleod-110',
    'tmpl-mcleod-wfai-001',
    'stops[*].notes',
    'notes',
    'ArrayFlatten',
    '{"SourceArrayPath":"stops","ItemField":"notes","FilterEmpty":true}',
    1000,
    false,
    NULL,
    NOW(), NOW()
),

-- Substring: extract year from McLeod pickup_date (first 4 chars = YYYY)
(
    'fm-mcleod-111',
    'tmpl-mcleod-wfai-001',
    'pickup_date',
    'pickupYear',
    'Substring',
    '{"StartIndex":0,"Length":4}',
    1010,
    false,
    '[{"Type":"Format","Pattern":"^\\d{4}$","ErrorMessage":"Pickup year must be a 4-digit number"}]',
    NOW(), NOW()
),

-- Math: calculate total weight across stops (if stored as flat field)
(
    'fm-mcleod-112',
    'tmpl-mcleod-wfai-001',
    'total_weight',
    'totalWeight',
    'Math',
    '{"Operation":"Round","DecimalPlaces":2}',
    1020,
    false,
    '[{"Type":"Range","MinValue":0,"ErrorMessage":"Total weight cannot be negative"}]',
    NOW(), NOW()
),

-- Conditional: set priority flag based on order mode
(
    'fm-mcleod-113',
    'tmpl-mcleod-wfai-001',
    'order_mode',
    'isExpedited',
    'Conditional',
    '{"Conditions":[{"SourceValue":"X","TargetValue":true},{"SourceValue":"A","TargetValue":true}],"DefaultValue":false}',
    1030,
    false,
    NULL,
    NOW(), NOW()
),

-- Template: build a human-readable order summary string
(
    'fm-mcleod-114',
    'tmpl-mcleod-wfai-001',
    'id',
    'orderSummary',
    'Template',
    '{"TemplateString":"Order {id} from {customer.name} - Status: {status} - Mode: {order_mode}"}',
    1040,
    false,
    '[{"Type":"Length","MaxLength":500,"ErrorMessage":"Order summary cannot exceed 500 characters"}]',
    NOW(), NOW()
)

ON CONFLICT (id) DO NOTHING;

COMMIT;

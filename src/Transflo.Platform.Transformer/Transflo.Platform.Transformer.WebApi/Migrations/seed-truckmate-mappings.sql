-- Seed data for TruckMate to WFAI transformation
-- This script creates a complete mapping template for transforming TruckMate JSON to WFAI format

-- Step 1: Ensure TruckMate TMS system exists
INSERT INTO tms_systems (id, name, display_name, description, version, is_active, created_at, updated_at)
VALUES (
    'tms-truckmate-001',
    'TruckMate',
    'TruckMate TMS',
    'TruckMate Transportation Management System',
    '1.0',
    true,
    NOW(),
    NOW()
)
ON CONFLICT (id) DO NOTHING;

-- Step 2: Create the mapping template
INSERT INTO field_mapping_templates (
    template_id, name, description, tms_system_id, version,
    status, created_at, updated_at, created_by
)
VALUES (
    'tmpl-truckmate-wfai-001',
    'TruckMate to WFAI Transformation',
    'Complete field mapping template for transforming TruckMate order data to WFAI format',
    'tms-truckmate-001',
    1,
    'Published',
    NOW(),
    NOW(),
    'system'
)
ON CONFLICT (template_id, version) DO NOTHING;

-- Step 3: Create field mappings for root-level fields
INSERT INTO field_mappings (id, template_id, source_path, target_path, transformation_type, execution_order, is_required, created_at, updated_at)
VALUES
    -- Basic order information
    ('fm-001', 'tmpl-truckmate-wfai-001', 'orderId', 'externalId', 'Direct', 1, true, NOW(), NOW()),
    ('fm-002', 'tmpl-truckmate-wfai-001', 'statusDescription', 'status', 'Direct', 2, true, NOW(), NOW()),
    ('fm-003', 'tmpl-truckmate-wfai-001', 'totalCharges', 'totalAmount', 'Direct', 3, true, NOW(), NOW()),

    -- Pickup stop - name
    ('fm-010', 'tmpl-truckmate-wfai-001', 'pickupAt.name', 'stops[0].name', 'Direct', 10, true, NOW(), NOW()),
    ('fm-011', 'tmpl-truckmate-wfai-001', 'pickupAt.clientId', 'stops[0].externalId', 'Direct', 11, false, NOW(), NOW()),
    ('fm-012', 'tmpl-truckmate-wfai-001', 'actualPickup', 'stops[0].actualArrival', 'Direct', 12, true, NOW(), NOW()),

    -- Pickup location
    ('fm-020', 'tmpl-truckmate-wfai-001', 'pickupAt.name', 'stops[0].location.name', 'Direct', 20, true, NOW(), NOW()),
    ('fm-021', 'tmpl-truckmate-wfai-001', 'pickupAt.address1', 'stops[0].location.address1', 'Direct', 21, true, NOW(), NOW()),
    ('fm-022', 'tmpl-truckmate-wfai-001', 'pickupAt.address2', 'stops[0].location.address2', 'Direct', 22, false, NOW(), NOW()),
    ('fm-023', 'tmpl-truckmate-wfai-001', 'pickupAt.city', 'stops[0].location.city', 'Direct', 23, true, NOW(), NOW()),
    ('fm-024', 'tmpl-truckmate-wfai-001', 'pickupAt.province', 'stops[0].location.state', 'Direct', 24, true, NOW(), NOW()),
    ('fm-025', 'tmpl-truckmate-wfai-001', 'pickupAt.country', 'stops[0].location.country', 'Direct', 25, true, NOW(), NOW()),
    ('fm-026', 'tmpl-truckmate-wfai-001', 'pickupAt.postalCode', 'stops[0].location.zipCode', 'Direct', 26, true, NOW(), NOW()),

    -- Pickup tractor and trailer
    ('fm-030', 'tmpl-truckmate-wfai-001', 'pickupPowerUnit1', 'stops[0].tractor.number', 'Direct', 30, false, NOW(), NOW()),
    ('fm-031', 'tmpl-truckmate-wfai-001', 'pickupTrailer1', 'stops[0].trailers[0].number', 'Direct', 31, false, NOW(), NOW()),
    ('fm-032', 'tmpl-truckmate-wfai-001', 'isDangerousGoods', 'stops[0].trailers[0].hasHazardousMaterials', 'Direct', 32, false, NOW(), NOW()),

    -- Pickup freight
    ('fm-040', 'tmpl-truckmate-wfai-001', 'billNumber', 'stops[0].freight[0].order.externalId', 'Direct', 40, true, NOW(), NOW()),
    ('fm-041', 'tmpl-truckmate-wfai-001', 'commodity', 'stops[0].freight[0].commodities[0].label', 'Direct', 41, false, NOW(), NOW()),
    ('fm-042', 'tmpl-truckmate-wfai-001', 'piecesUnits', 'stops[0].freight[0].commodities[0].unit', 'Direct', 42, false, NOW(), NOW()),
    ('fm-043', 'tmpl-truckmate-wfai-001', 'pieces', 'stops[0].freight[0].commodities[0].amount', 'Direct', 43, false, NOW(), NOW()),

    -- Pickup billTo
    ('fm-050', 'tmpl-truckmate-wfai-001', 'billToCustomer.clientId', 'stops[0].billTos[0].externalId', 'Direct', 50, true, NOW(), NOW()),
    ('fm-051', 'tmpl-truckmate-wfai-001', 'billToCustomer.name', 'stops[0].billTos[0].name', 'Direct', 51, true, NOW(), NOW()),
    ('fm-052', 'tmpl-truckmate-wfai-001', 'billToCustomer.address1', 'stops[0].billTos[0].location.address1', 'Direct', 52, false, NOW(), NOW()),
    ('fm-053', 'tmpl-truckmate-wfai-001', 'billToCustomer.address2', 'stops[0].billTos[0].location.address2', 'Direct', 53, false, NOW(), NOW()),
    ('fm-054', 'tmpl-truckmate-wfai-001', 'billToCustomer.city', 'stops[0].billTos[0].location.city', 'Direct', 54, false, NOW(), NOW()),
    ('fm-055', 'tmpl-truckmate-wfai-001', 'billToCustomer.province', 'stops[0].billTos[0].location.state', 'Direct', 55, false, NOW(), NOW()),
    ('fm-056', 'tmpl-truckmate-wfai-001', 'billToCustomer.country', 'stops[0].billTos[0].location.country', 'Direct', 56, false, NOW(), NOW()),
    ('fm-057', 'tmpl-truckmate-wfai-001', 'billToCustomer.postalCode', 'stops[0].billTos[0].location.zipCode', 'Direct', 57, false, NOW(), NOW()),
    ('fm-058', 'tmpl-truckmate-wfai-001', 'billToCustomer.phoneNumber', 'stops[0].billTos[0].contacts[0].phoneNumber', 'Direct', 58, false, NOW(), NOW()),
    ('fm-059', 'tmpl-truckmate-wfai-001', 'billToCustomer.email', 'stops[0].billTos[0].contacts[0].email', 'Direct', 59, false, NOW(), NOW()),

    -- Delivery stop - name
    ('fm-100', 'tmpl-truckmate-wfai-001', 'consignee.name', 'stops[1].name', 'Direct', 100, true, NOW(), NOW()),
    ('fm-101', 'tmpl-truckmate-wfai-001', 'consignee.clientId', 'stops[1].externalId', 'Direct', 101, false, NOW(), NOW()),
    ('fm-102', 'tmpl-truckmate-wfai-001', 'deliverBy', 'stops[1].scheduledEarlyArrival', 'Direct', 102, false, NOW(), NOW()),
    ('fm-103', 'tmpl-truckmate-wfai-001', 'actualDelivery', 'stops[1].actualArrival', 'Direct', 103, true, NOW(), NOW()),

    -- Delivery location
    ('fm-110', 'tmpl-truckmate-wfai-001', 'consignee.name', 'stops[1].location.name', 'Direct', 110, true, NOW(), NOW()),
    ('fm-111', 'tmpl-truckmate-wfai-001', 'consignee.address1', 'stops[1].location.address1', 'Direct', 111, true, NOW(), NOW()),
    ('fm-112', 'tmpl-truckmate-wfai-001', 'consignee.address2', 'stops[1].location.address2', 'Direct', 112, false, NOW(), NOW()),
    ('fm-113', 'tmpl-truckmate-wfai-001', 'consignee.city', 'stops[1].location.city', 'Direct', 113, true, NOW(), NOW()),
    ('fm-114', 'tmpl-truckmate-wfai-001', 'consignee.province', 'stops[1].location.state', 'Direct', 114, true, NOW(), NOW()),
    ('fm-115', 'tmpl-truckmate-wfai-001', 'consignee.country', 'stops[1].location.country', 'Direct', 115, true, NOW(), NOW()),
    ('fm-116', 'tmpl-truckmate-wfai-001', 'consignee.postalCode', 'stops[1].location.zipCode', 'Direct', 116, true, NOW(), NOW()),

    -- Delivery trailer
    ('fm-120', 'tmpl-truckmate-wfai-001', 'deliveryTrailer1', 'stops[1].trailers[0].number', 'Direct', 120, false, NOW(), NOW()),
    ('fm-121', 'tmpl-truckmate-wfai-001', 'isDangerousGoods', 'stops[1].trailers[0].hasHazardousMaterials', 'Direct', 121, false, NOW(), NOW()),

    -- Delivery freight
    ('fm-130', 'tmpl-truckmate-wfai-001', 'billNumber', 'stops[1].freight[0].order.externalId', 'Direct', 130, true, NOW(), NOW()),
    ('fm-131', 'tmpl-truckmate-wfai-001', 'piecesUnits', 'stops[1].freight[0].commodities[0].unit', 'Direct', 131, false, NOW(), NOW()),

    -- Delivery billTo (same as pickup)
    ('fm-150', 'tmpl-truckmate-wfai-001', 'billToCustomer.clientId', 'stops[1].billTos[0].externalId', 'Direct', 150, true, NOW(), NOW()),
    ('fm-151', 'tmpl-truckmate-wfai-001', 'billToCustomer.name', 'stops[1].billTos[0].name', 'Direct', 151, true, NOW(), NOW()),
    ('fm-152', 'tmpl-truckmate-wfai-001', 'billToCustomer.address1', 'stops[1].billTos[0].location.address1', 'Direct', 152, false, NOW(), NOW()),
    ('fm-153', 'tmpl-truckmate-wfai-001', 'billToCustomer.address2', 'stops[1].billTos[0].location.address2', 'Direct', 153, false, NOW(), NOW()),
    ('fm-154', 'tmpl-truckmate-wfai-001', 'billToCustomer.city', 'stops[1].billTos[0].location.city', 'Direct', 154, false, NOW(), NOW()),
    ('fm-155', 'tmpl-truckmate-wfai-001', 'billToCustomer.province', 'stops[1].billTos[0].location.state', 'Direct', 155, false, NOW(), NOW()),
    ('fm-156', 'tmpl-truckmate-wfai-001', 'billToCustomer.country', 'stops[1].billTos[0].location.country', 'Direct', 156, false, NOW(), NOW()),
    ('fm-157', 'tmpl-truckmate-wfai-001', 'billToCustomer.postalCode', 'stops[1].billTos[0].location.zipCode', 'Direct', 157, false, NOW(), NOW()),
    ('fm-158', 'tmpl-truckmate-wfai-001', 'billToCustomer.phoneNumber', 'stops[1].billTos[0].contacts[0].phoneNumber', 'Direct', 158, false, NOW(), NOW()),
    ('fm-159', 'tmpl-truckmate-wfai-001', 'billToCustomer.email', 'stops[1].billTos[0].contacts[0].email', 'Direct', 159, false, NOW(), NOW())
ON CONFLICT (id) DO NOTHING;

-- Note: For complex array transformations (traceNumbers to references),
-- you may need to implement custom transformation logic in the TransformationService
-- or use ArrayMap transformation type when it's fully implemented.

COMMIT;

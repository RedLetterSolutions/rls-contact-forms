-- Verify Sites Seeding
-- Run this query to see all seeded sites

SELECT 
    site_id,
    name,
    to_email,
    from_email,
    redirect_url,
    allowed_origins,
    is_active,
    created_at
FROM sites
ORDER BY site_id;

-- Count of active sites
SELECT COUNT(*) as active_sites FROM sites WHERE is_active = true;

-- Detailed view for each site
SELECT 
    '=== ' || name || ' ===' as site_info,
    'Site ID: ' || site_id as api_endpoint,
    'To Email: ' || to_email as recipient,
    'From Email: ' || from_email as sender,
    'Redirect: ' || redirect_url as success_page,
    'Origins: ' || allowed_origins as cors_whitelist,
    CASE WHEN is_active THEN '✓ Active' ELSE '✗ Inactive' END as status
FROM sites
ORDER BY name;

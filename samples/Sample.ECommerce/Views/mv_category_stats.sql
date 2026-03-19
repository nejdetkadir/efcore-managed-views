-- @viewName: mv_category_stats
-- @schema: public
-- @type: materialized
-- @dependsOn: vw_active_products
-- @indexes: idx_cat_stats_category(category_name)

SELECT
    category_name,
    COUNT(*) AS product_count,
    AVG(price) AS avg_price,
    MIN(price) AS min_price,
    MAX(price) AS max_price
FROM vw_active_products
GROUP BY category_name

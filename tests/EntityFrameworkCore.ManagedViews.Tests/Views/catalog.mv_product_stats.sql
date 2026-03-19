-- @type: materialized
-- @dependsOn: vw_test_products
-- @indexes: idx_category(category_id)
SELECT category_id, COUNT(*) AS product_count FROM products GROUP BY category_id

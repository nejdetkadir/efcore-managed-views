-- @viewName: vw_active_products
-- @schema: public
-- @type: view

SELECT
    p.id,
    p.name,
    p.price,
    c.name AS category_name
FROM products p
INNER JOIN categories c ON p.category_id = c.id
WHERE p.is_active = true

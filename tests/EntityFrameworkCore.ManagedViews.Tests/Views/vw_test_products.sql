-- @viewName: vw_test_products
-- @schema: public
-- @type: view
SELECT id, name, price FROM products WHERE active = true

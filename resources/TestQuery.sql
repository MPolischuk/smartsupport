SELECT *
FROM dbo.Orders O
WHERE O.Id = 'AT-1003-R'

SELECT *
FROM dbo.OrderEvents OE
WHERE OE.OrderId = 'AT-1003-R'
If Not Exists (Select * from Information_Schema.Columns
Where Table_Name = 'Product'
And Column_Name = 'UnleashGuid')
Begin
Alter Table [Product]
Add UnleashGuid nvarchar(50) Null
End

If Not Exists (Select * from Information_Schema.Columns
Where Table_Name = 'Order'
And Column_Name = 'SalesOrderUnleashGuid')
Begin
Alter Table [Order]
Add SalesOrderUnleashGuid nvarchar(50) Null
End

If Not Exists (Select * from Information_Schema.Columns
Where Table_Name = 'Product'
And Column_Name = 'StripeProductId')
Begin
Alter Table [Product]
Add StripeProductId nvarchar(50) Null
End

If Not Exists (Select * from Information_Schema.Columns
Where Table_Name = 'Category'
And Column_Name = 'UnleashProductGroupGuId')
Begin
Alter Table [Category]
Add UnleashProductGroupGuId nvarchar(50) Null
End

If Not Exists (Select * from Information_Schema.Columns
Where Table_Name = 'Product'
And Column_Name = 'StripePriceId')
Begin
Alter Table [Product]
Add StripePriceId nvarchar(50) Null
End

If Not Exists (Select * from Information_Schema.Columns
Where Table_Name = 'Customer'
And Column_Name = 'StripeCustomerId')
Begin
Alter Table [Customer]
Add StripeCustomerId nvarchar(50) Null
End
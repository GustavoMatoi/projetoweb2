UPDATE ur
SET ur.RoleId = (SELECT Id FROM AspNetRoles WHERE Name = 'Nutricionista')
FROM AspNetUserRoles ur
JOIN AspNetUsers u ON u.Id = ur.UserId
WHERE u.Email = 'gustavo_nutri@gmail.com';
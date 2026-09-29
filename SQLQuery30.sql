SELECT p.IdProfissional, p.Nome, u.Email, r.Name AS Role
FROM tbProfissional p
JOIN AspNetUsers u ON u.Id = p.IdUser
JOIN AspNetUserRoles ur ON ur.UserId = u.Id
JOIN AspNetRoles r ON r.Id = ur.RoleId
ORDER BY p.IdProfissional;
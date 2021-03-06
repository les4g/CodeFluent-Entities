using System;
using CodeFluent.Model;
using CodeFluent.Model.Code;

namespace SoftFluent.AspNetIdentity
{
    public class IdentityRole
    {
        public IdentityRole(Entity entity)
        {
            if (entity == null) throw new ArgumentNullException("entity");

            Entity = entity;

            KeyProperty = ProjectUtilities.FindByPropertyType(Entity, PropertyType.RoleKey);
            NameProperty = ProjectUtilities.FindByPropertyType(Entity, PropertyType.RoleName) ?? ProjectUtilities.FindNameProperty(entity);
            UsersProperty = ProjectUtilities.FindByPropertyType(Entity, PropertyType.RoleUsers);
            ClaimsProperty = ProjectUtilities.FindByPropertyType(Entity, PropertyType.RoleClaims);

            LoadByKeyMethod = ProjectUtilities.FindByMethodType(Entity, MethodType.LoadRoleByKey);
            if (LoadByKeyMethod != null)
            {
                LoadByKeyMethodName = LoadByKeyMethod.Name;
            }
            else if (KeyProperty != null && Entity.LoadByKeyMethod != null)
            {
                LoadByKeyMethodName = Entity.LoadByKeyMethod.Name;
            }
            else
            {
                LoadByKeyMethodName = "LoadByEntityKey";
            }

            LoadByNameMethod = ProjectUtilities.FindByMethodType(Entity, MethodType.LoadRoleByName) ?? Entity.LoadByCollectionKeyMethod;
            LoadAllMethod = ProjectUtilities.FindByMethodType(Entity, MethodType.LoadAllRoles) ?? Entity.LoadAllMethod;
        }

        public Entity Entity { get; set; }

        public Property KeyProperty { get; private set; }
        public Property NameProperty { get; private set; }
        public Property UsersProperty { get; private set; }
        public Property ClaimsProperty { get; private set; }

        public Method LoadByKeyMethod { get; private set; }
        public string LoadByKeyMethodName { get; private set; }
        public Method LoadByNameMethod { get; private set; }
        public Method LoadAllMethod { get; private set; }

        public string KeyTypeName
        {
            get
            {
                if (KeyProperty != null)
                    return KeyProperty.ClrFullTypeName;

                return typeof(string).FullName; // EntityKey
            }
        }

        public bool IsStringId
        {
            get { return KeyProperty == null || KeyProperty.ClrFullTypeName == typeof(string).FullName; }
        }

        public string KeyPropertyName
        {
            get
            {
                if (KeyProperty != null)
                    return KeyProperty.Name;

                return "EntityKey";
            }
        }
    }
}
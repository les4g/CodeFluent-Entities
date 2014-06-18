using System;
using System.Linq;
using System.Windows.Forms;
using CodeFluent.Model;
using CodeFluent.Model.Code;
using CodeFluent.Runtime.UI;

namespace SoftFluent.AspNetIdentity
{
    public partial class ConfigurationForm : Form
    {
        private readonly Project _project;

        public ConfigurationForm(Project project)
        {
            if (project == null)
                throw new ArgumentNullException("project");

            _project = project;

            InitializeComponent();

            InitializeEntities();
            UpdateControls();
        }

        private void InitializeEntities()
        {

            Entity userEntity = ProjectUtilities.FindByEntityType(_project, EntityType.User);
            Entity claimEntity = ProjectUtilities.FindByEntityType(_project, EntityType.Claim);
            Entity loginEntity = ProjectUtilities.FindByEntityType(_project, EntityType.Login);
            Entity roleEntity = ProjectUtilities.FindByEntityType(_project, EntityType.Role);
            Entity userRoleEntity = ProjectUtilities.FindByEntityType(_project, EntityType.UserRole);
            var entity = userEntity ?? claimEntity ?? loginEntity ?? roleEntity ?? userRoleEntity;

            Property emailProperty = userEntity == null ? null : ProjectUtilities.FindByPropertyType(userEntity, PropertyType.Email);
            Property phoneNumberProperty = userEntity == null ? null : ProjectUtilities.FindByPropertyType(userEntity, PropertyType.PhoneNumber);

            foreach (Namespace ns in _project.AllNamespaces)
            {
                comboBoxNamespace.Items.Add(ns.FullName);
            }

            // Set namespace
            if (entity != null)
            {
                comboBoxNamespace.Text = entity.Namespace;
            }
            else
            {
                comboBoxNamespace.Text = _project.DefaultNamespace;
            }

            // initialize checkbox
            checkBoxClaims.Checked = claimEntity != null;
            checkBoxExternalLogins.Checked = loginEntity != null;
            checkBoxRole.Checked = roleEntity != null;
            //checkBoxUserRole.Checked = userRoleEntity != null;
            checkBoxEmailUnique.Checked = emailProperty != null && emailProperty.IsUnique;
            checkBoxPhoneNumberUnique.Checked = phoneNumberProperty != null && phoneNumberProperty.IsUnique;
        }

        void UpdateControls()
        {
            textBoxClaimsEntityName.Enabled = checkBoxClaims.Checked;
            textBoxExternalLoginsEntityName.Enabled = checkBoxExternalLogins.Checked;
            textBoxRoleEntityName.Enabled = checkBoxRole.Checked;
            //textBoxUserRoleEntityName.Enabled = checkBoxUserRole.Checked;
            buttonOk.Enabled = IsValid();
        }

        bool IsValid()
        {
            if (checkBoxClaims.Checked && string.IsNullOrWhiteSpace(textBoxClaimsEntityName.Text))
                return false;

            if (checkBoxExternalLogins.Checked && string.IsNullOrWhiteSpace(textBoxExternalLoginsEntityName.Text))
                return false;

            if (checkBoxRole.Checked && string.IsNullOrWhiteSpace(textBoxRoleEntityName.Text))
                return false;

            //if (checkBoxUserRole.Checked && string.IsNullOrWhiteSpace(textBoxUserRoleEntityName.Text))
            //    return false;

            if (string.IsNullOrWhiteSpace(textBoxUserEntityName.Text))
                return false;

            if (string.IsNullOrWhiteSpace(comboBoxNamespace.Text))
                return false;

            return true;
        }

        private void checkBoxRole_CheckedChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void textBoxRoleEntityName_TextChanged(object sender, EventArgs e)
        {
            UpdateControls();
        }

        private void buttonOk_Click(object sender, EventArgs e)
        {
            Entity userEntity = CreateUserEntity();
            Entity roleEntity = null;
            Entity userRoleEntity = null;
            Entity loginsEntity = null;
            Entity claimsEntity = null;

            if (checkBoxRole.Checked)
            {
                roleEntity = CreateRoleEntity();
            }

            //if (checkBoxUserRole.Checked)
            //{
            //    userRoleEntity = CreateUserRoleEntity();
            //    AddRelation(userEntity, PropertyType.Roles, userRoleEntity, PropertyType.User, RelationType.ManyToOne);
            //    AddRelation(roleEntity, PropertyType.Users, userRoleEntity, PropertyType.Role, RelationType.ManyToOne);
            //}
            //else
            //{
                AddRelation(userEntity, PropertyType.Roles, roleEntity, PropertyType.Users, RelationType.ManyToMany);
            //}

            if (checkBoxClaims.Checked)
            {
                claimsEntity = CreateClaimsEntity();
                AddRelation(userEntity, PropertyType.Claims, claimsEntity, PropertyType.User, RelationType.ManyToOne);
            }

            if (checkBoxExternalLogins.Checked)
            {
                loginsEntity = CreateLoginsEntity();
                AddRelation(userEntity, PropertyType.Logins, loginsEntity, PropertyType.User, RelationType.ManyToOne);
            }

            CreateUserMethods(userEntity, loginsEntity);

            SetCollectionMode(userEntity);
            SetCollectionMode(roleEntity);
            SetCollectionMode(userRoleEntity);
            SetCollectionMode(loginsEntity);
            SetCollectionMode(claimsEntity);

            this.Close();
        }

        private void SetCollectionMode(Entity entity)
        {
            if (entity == null)
                return;

            int keyPropertyCount = entity.Properties.Count(_ => _.IsKey);
            if (keyPropertyCount > 1)
            {
                entity.SetType = SetType.List;
            }
        }

        public static void AddRelation(Entity fromEntity, PropertyType fromPropertyType, Entity toEntity, PropertyType toPropertyType, RelationType relationType)
        {
            if (fromEntity == null || toEntity == null)
                return;

            Property fromProperty = ProjectUtilities.FindByPropertyType(fromEntity, fromPropertyType);
            Property toProperty = ProjectUtilities.FindByPropertyType(toEntity, toPropertyType);

            if (fromProperty == null || toProperty == null)
                return;

            switch (relationType)
            {
                case RelationType.OneToOne:
                    fromProperty.TypeName = toEntity.ClrFullTypeName;
                    toProperty.TypeName = fromEntity.ClrFullTypeName;
                    break;
                case RelationType.OneToMany:
                    fromProperty.TypeName = toEntity.ClrFullTypeName;
                    toProperty.TypeName = fromEntity.Set.ClrFullTypeName;
                    break;
                case RelationType.ManyToOne:
                    fromProperty.TypeName = toEntity.Set.ClrFullTypeName;
                    toProperty.TypeName = fromEntity.ClrFullTypeName;
                    break;
                case RelationType.ManyToMany:
                    fromProperty.TypeName = toEntity.Set.ClrFullTypeName;
                    toProperty.TypeName = fromEntity.Set.ClrFullTypeName;
                    break;
            }

            fromProperty.SetRelation(toProperty, relationType);

            switch (relationType)
            {
                case RelationType.OneToMany:
                    toProperty.CascadeDelete = CascadeType.Before;
                    break;
                case RelationType.ManyToOne:
                    fromProperty.CascadeDelete = CascadeType.Before;
                    break;
            }
        }

        private Entity CreateUserEntity()
        {
            Entity entity = GetOrCreateEntity(EntityType.User, textBoxUserEntityName.Text, comboBoxNamespace.Text);
            foreach (var typeProperty in TypeProperty.UserProperties)
            {
                if (!MustGenerate(EntityType.User, typeProperty))
                    continue;

                Property property = GetOrCreateProperty(entity, typeProperty);
                if (typeProperty.IdentityPropertyType == PropertyType.UserKey)
                {
                    property.IsKey = true;
                }
                else if (typeProperty.IdentityPropertyType == PropertyType.UserName)
                {
                    property.IsCollectionKey = true;
                }
                else if (typeProperty.IdentityPropertyType == PropertyType.Email)
                {
                    property.EditorIsUnique = checkBoxEmailUnique.Checked;
                }
                else if (typeProperty.IdentityPropertyType == PropertyType.PhoneNumber)
                {
                    property.EditorIsUnique = checkBoxPhoneNumberUnique.Checked;
                }
            }

            return entity;
        }

        private void CreateUserMethods(Entity userEntity, Entity loginEntity)
        {
            if (userEntity == null) throw new ArgumentNullException("userEntity");

            // LoadUserByProviderKey
            if (loginEntity != null)
            {
                var loginsProperty = ProjectUtilities.FindByPropertyType(userEntity, PropertyType.Logins);
                var providerKeyProperty = ProjectUtilities.FindByPropertyType(loginEntity, PropertyType.LoginProviderKey);
                if (loginsProperty != null && providerKeyProperty != null)
                {
                    Method loadByProviderKeyMethod = ProjectUtilities.FindByMethodType(userEntity, MethodType.LoadUserByProviderKey);
                    if (loadByProviderKeyMethod == null)
                    {
                        loadByProviderKeyMethod = new Method();
                        loadByProviderKeyMethod.Name = "LoadByProviderKey";
                        loadByProviderKeyMethod.SetAttributeValue("", "methodType", Constants.NamespaceUri, MethodType.LoadUserByProviderKey);
                        userEntity.Methods.Add(loadByProviderKeyMethod);

                        Body body = new Body();
                        body.Text = string.Format("LOADONE(string providerKey) WHERE {0}.{1} = @providerKey", loginsProperty.Name, providerKeyProperty.Name);

                        loadByProviderKeyMethod.Bodies.Add(body);
                    }
                }
            }

            // LoadUserByEmail
            var emailProperty = ProjectUtilities.FindByPropertyType(userEntity, PropertyType.Email);
            if (emailProperty != null)
            {
                Method loadByEmailMethod = ProjectUtilities.FindByMethodType(userEntity, MethodType.LoadUserByEmail);
                if (loadByEmailMethod == null)
                {
                    loadByEmailMethod = new Method();
                    loadByEmailMethod.Name = "LoadByEmail";
                    loadByEmailMethod.SetAttributeValue("", "methodType", Constants.NamespaceUri, MethodType.LoadUserByEmail);
                    userEntity.Methods.Add(loadByEmailMethod);

                    Body body = new Body();
                    body.Text = string.Format("LOADONE({0}) WHERE {0} = @{0}", emailProperty.Name);

                    loadByEmailMethod.Bodies.Add(body);
                }
            }
        }

        private Entity CreateRoleEntity()
        {
            Entity entity = GetOrCreateEntity(EntityType.Role, textBoxRoleEntityName.Text, comboBoxNamespace.Text);
            foreach (var typeProperty in TypeProperty.RoleProperties)
            {
                if (!MustGenerate(EntityType.User, typeProperty))
                    continue;

                Property property = GetOrCreateProperty(entity, typeProperty);
                if (typeProperty.IdentityPropertyType == PropertyType.RoleKey)
                {
                    property.IsKey = true;
                }
                else if (typeProperty.IdentityPropertyType == PropertyType.RoleName)
                {
                    property.IsCollectionKey = true;
                }
            }

            return entity;
        }

        //private Entity CreateUserRoleEntity()
        //{
        //    Entity entity = GetOrCreateEntity(EntityType.UserRole, textBoxUserRoleEntityName.Text, comboBoxNamespace.Text);
        //    foreach (var typeProperty in TypeProperty.UserRoleProperties)
        //    {
        //        if (!MustGenerate(EntityType.User, typeProperty))
        //            continue;

        //        Property property = GetOrCreateProperty(entity, typeProperty);
        //        property.IsKey = true;
        //    }

        //    return entity;
        //}

        private Entity CreateClaimsEntity()
        {
            Entity entity = GetOrCreateEntity(EntityType.Claim, textBoxClaimsEntityName.Text, comboBoxNamespace.Text);
            foreach (var typeProperty in TypeProperty.ClaimsProperties)
            {
                if (!MustGenerate(EntityType.Claim, typeProperty))
                    continue;

                Property property = GetOrCreateProperty(entity, typeProperty);

                if (typeProperty.IdentityPropertyType == PropertyType.ClaimsKey)
                {
                    property.IsKey = true;
                }
            }

            Method deleteMethod = ProjectUtilities.FindByMethodType(entity, MethodType.DeleteClaim);
            if (deleteMethod == null)
            {
                deleteMethod = new Method();
                deleteMethod.Name = "DeleteByTypeAndValue";
                deleteMethod.SetAttributeValue("", "methodType", Constants.NamespaceUri, MethodType.DeleteClaim);
                entity.Methods.Add(deleteMethod);

                Body body = new Body();
                string typePropertyName = ProjectUtilities.FindByPropertyType(entity, PropertyType.ClaimsType).Name;
                string valuePropertyName = ProjectUtilities.FindByPropertyType(entity, PropertyType.ClaimsValue).Name;
                body.Text = string.Format("DELETE({0}, {1}) WHERE {0} = @{0} AND {1} = @{1}", typePropertyName, valuePropertyName);

                deleteMethod.Bodies.Add(body);
            }

            return entity;
        }

        private Entity CreateLoginsEntity()
        {
            Entity entity = GetOrCreateEntity(EntityType.Login, textBoxExternalLoginsEntityName.Text, comboBoxNamespace.Text);
            foreach (var typeProperty in TypeProperty.ExternalLoginProperties)
            {
                if (!MustGenerate(EntityType.Login, typeProperty))
                    continue;

                Property property = GetOrCreateProperty(entity, typeProperty);
                if (typeProperty.IdentityPropertyType == PropertyType.UserKey)
                {
                    property.IsKey = true;
                }
            }


            Method deleteMethod = ProjectUtilities.FindByMethodType(entity, MethodType.DeleteClaim);
            if (deleteMethod == null)
            {
                deleteMethod = new Method();
                deleteMethod.Name = "DeleteByUserAndProviderKey";
                deleteMethod.SetAttributeValue("", "methodType", Constants.NamespaceUri, MethodType.DeleteLogin);
                entity.Methods.Add(deleteMethod);

                Body body = new Body();
                string userPropertyName = ProjectUtilities.FindByPropertyType(entity, PropertyType.User).Name;
                string providerKeyPropertyName = ProjectUtilities.FindByPropertyType(entity, PropertyType.LoginProviderKey).Name;
                body.Text = string.Format("DELETE({0}, {1}) WHERE {0} = @{0} AND {1} = @{1}", userPropertyName, providerKeyPropertyName);

                deleteMethod.Bodies.Add(body);
            }


            return entity;
        }

        private bool MustGenerate(EntityType entityType, TypeProperty property)
        {
            switch (entityType)
            {
                case EntityType.User:
                    switch (property.IdentityPropertyType)
                    {
                        case PropertyType.Roles:
                            return checkBoxRole.Checked;
                        case PropertyType.Claims:
                            return checkBoxClaims.Checked;
                        case PropertyType.Logins:
                            return checkBoxExternalLogins.Checked;
                    }
                    break;
            }

            return true;
        }

        private Entity GetOrCreateEntity(EntityType entityType, string entityName, string @namespace)
        {
            Entity entity = _project.Entities.Find(entityName);
            if (entity == null)
            {
                entity = ProjectUtilities.FindByEntityType(_project, entityType);
                if (entity != null)
                {
                    entity.Name = entityName; // Rename
                }
            }

            if (entity == null)
            {
                entity = new Entity();
                entity.Name = entityName;
                entity.Namespace = @namespace;
                entity.SetAttributeValue("", "entityType", Constants.NamespaceUri, entityType);
                _project.Entities.Add(entity);
            }

            return entity;
        }

        private Property GetOrCreateProperty(Entity entity, TypeProperty typeProperty)
        {
            Property property = entity.Properties.Find(typeProperty.CanonicalName, StringComparison.OrdinalIgnoreCase);
            if (property == null)
            {
                property = ProjectUtilities.FindByPropertyType(entity, typeProperty.IdentityPropertyType);
                if (property != null)
                {
                    property.Name = typeProperty.CanonicalName;
                }
            }

            if (property == null)
            {
                property = new Property();
                property.Name = typeProperty.CanonicalName;
                property.TypeName = typeProperty.ExpectedType;
                if (typeProperty.ExpectedUIType != UIType.Unspecified)
                {
                    property.UIType = typeProperty.ExpectedUIType;
                }

                property.IsNullable = typeProperty.Nullable;

                if (!property.IsNullable && (
                    property.ClrFullTypeName == typeof(int).FullName ||
                    property.ClrFullTypeName == typeof(uint).FullName ||
                    property.ClrFullTypeName == typeof(short).FullName ||
                    property.ClrFullTypeName == typeof(ushort).FullName ||
                    property.ClrFullTypeName == typeof(long).FullName ||
                    property.ClrFullTypeName == typeof(ulong).FullName ||
                    property.ClrFullTypeName == typeof(byte).FullName ||
                    property.ClrFullTypeName == typeof(sbyte).FullName ||
                    property.ClrFullTypeName == typeof(float).FullName ||
                    property.ClrFullTypeName == typeof(decimal).FullName ||
                    property.ClrFullTypeName == typeof(double).FullName ||
                    property.ClrFullTypeName == typeof(DateTime).FullName ||
                    property.ClrFullTypeName == typeof(TimeSpan).FullName ||
                    property.ClrFullTypeName == typeof(DateTimeOffset).FullName ||
                    property.ClrFullTypeName == typeof(Guid).FullName ||
                    property.ClrFullTypeName == typeof(bool).FullName))
                {
                    property.MustUsePersistenceDefaultValue = false;
                }

                property.SetAttributeValue("", "propertyType", Constants.NamespaceUri, typeProperty.IdentityPropertyType);
                entity.Properties.Add(property);
            }

            return property;
        }
    }
}
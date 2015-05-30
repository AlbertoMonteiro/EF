using System.Data.Entity.Core.Common.CommandTrees;
using System.Data.Entity.Core.Common.CommandTrees.ExpressionBuilder;
using System.Data.Entity.Core.Metadata.Edm;
using System.Linq;

namespace VisualStudioSummitDemo.Interceptors.MultiTenant.CommandHandlers
{
    public class DbUpdateCommandTreeHandler : CommandTreeHandlerBase
    {
        private const string COLUMN_NAME = "TenantId";

        protected override bool CanHandle(DbCommandTree command)
        {
            return command is DbUpdateCommandTree;
        }

        protected override DbCommandTree Handle(DbCommandTree command)
        {
            var updateCommandTree = command as DbUpdateCommandTree;

            var dbSetClauses = updateCommandTree.SetClauses.Cast<DbSetClause>().ToList();

            dbSetClauses.RemoveAll(HasColumn);

            var edmType = updateCommandTree.Target.VariableType.EdmType as EntityType;
            var column = edmType.DeclaredProperties.SingleOrDefault(x => x.Name.EndsWith("TenantId"));

            var biding = updateCommandTree.Target;
            var porEmpresa = biding.VariableType
                .Variable(biding.VariableName)
                .Property(column)
                .Equal(column.TypeUsage.Constant(MultiTenantInterceptor.TentantId));

            return new DbUpdateCommandTree(updateCommandTree.MetadataWorkspace,
                                               updateCommandTree.DataSpace,
                                               updateCommandTree.Target,
                                               updateCommandTree.Predicate.And(porEmpresa),
                                               dbSetClauses.Cast<DbModificationClause>().ToList().AsReadOnly(),
                                               updateCommandTree.Returning);
        }

        private static DbSetClause CreateSetClause(DbSetClause expression)
        {
            var propertyExpression = expression.Property as DbPropertyExpression;
            return DbExpressionBuilder.SetClause(propertyExpression, expression.Value.Accept(new MultiTenantQueryVisitor(MultiTenantInterceptor.TentantId)));
        }

        private static bool HasColumn(DbSetClause setClause)
        {
            var dbPropertyExpression = setClause.Property as DbPropertyExpression;
            return dbPropertyExpression.Property.Name == COLUMN_NAME && (setClause.Value as DbConstantExpression).Value.Equals(0L);
        }
    }
}
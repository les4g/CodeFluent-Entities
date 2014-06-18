using CodeFluent.Model;
using CodeFluent.Model.Persistence;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;

namespace SoftFluent.Samples.ReadOnLoad
{
    public class ReadOnLoadAspect : IProjectTemplate
    {
        public static readonly XmlDocument Descriptor;
        public const string Namespace = "http://www.softfluent.com/aspects/samples/read-on-load"; // this is my custom XML namespace
        private const string PassPhraseToken = "PassPhrase";
        public Project Project { get; set; }

        static ReadOnLoadAspect()
        {
            Descriptor = new XmlDocument();
            Descriptor.LoadXml(
@"<cf:project xmlns:cf='http://www.softfluent.com/codefluent/2005/1' defaultNamespace='ReadOnLoadAspect'>
    <cf:pattern name='Read On Load Aspect' namespaceUri='" + Namespace + @"' preferredPrefix='ca' step='Categories'>
       <cf:message class='_doc'> CodeFluent Sample ReadOnLoad Aspect Version 1.0.0.0 - 2014/06/18 This aspect modifies Save procedures in order to select read on load columns after inserting or updating.</cf:message>
    </cf:pattern>
  </cf:project>");
        }

        public XmlDocument Run(IDictionary context)
        {
            if (context == null || !context.Contains("Project"))
            {
                // we are probably called for meta data inspection, so we send back the descriptor xml<br />
                return Descriptor;
            }

            // the dictionary contains at least these two entries
            Project = (Project)context["Project"];

            foreach (var procedure in Project.Database.Procedures.Where(procedure => procedure.ProcedureType == CodeFluent.Model.Persistence.ProcedureType.SaveEntity))
            {
                UpdateProcedure(procedure);
            }


            // we have no specific Xml to send back, but aspect description
            return Descriptor;
        }

        private void UpdateProcedure(CodeFluent.Model.Persistence.Procedure procedure)
        {
            if (procedure.Table == null || procedure.Table.Entity == null)
                return;

            var columns = procedure.Table.Entity.Properties
                .Where(property => property.MustReadOnSave && !property.IsPersistenceIdentity)
                .SelectMany(property => property.Columns)
                .Where(column => column != null && column.IsRowVersion)
                .ToList();

            if (columns.Count == 0)
                return;

            procedure.Body.Visit<ProcedureStatement>(statement =>
            {
                var blockStatement = statement as ProcedureBlockStatement;
                if (blockStatement == null)
                    return;

                bool insertOrUpdate = false;

                foreach (var s in blockStatement.Statements)
                {
                    if (IsInsertOrUpdateStatement(s))
                    {
                        insertOrUpdate = true;
                    }

                    var selectStatement = s as ProcedureSelectStatement;
                    if (insertOrUpdate && selectStatement != null)
                    {
                        foreach (var column in columns)
                        {
                            var setStatment = new ProcedureSetStatement(selectStatement, new TableRefColumn(column));
                            selectStatement.Set.Add(setStatment);
                        }
                    }
                }
            });
        }

        private bool IsInsertOrUpdateStatement(ProcedureStatement statement)
        {
            if (statement == null)
                return false;

            var blockStatement = statement as ProcedureBlockStatement;
            if (blockStatement != null)
            {
                if (blockStatement.Statements.Count == 0)
                    return false;

                return blockStatement.Statements[0] is ProcedureInsertStatement || blockStatement.Statements[0] is ProcedureUpdateStatement;
            }

            return statement is ProcedureInsertStatement || statement is ProcedureUpdateStatement;
        }
    }
}

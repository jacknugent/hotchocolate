using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HotChocolate.Resolvers;
using HotChocolate.Tests;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Execution
{
    public class MiddlewareContextTests
    {
        [Fact]
        public async Task AccessVariables()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    "type Query { foo(bar: String) : String }")
                .AddResolver("Query", "foo", ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query abc($abc: String){ foo(bar: $abc) }")
                .SetVariableValue("abc", "def")
                .Create();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task AccessVariables_Fails_When_Variable_Not_Exists()
        {
            // arrange
            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    "type Query { foo(bar: String) : String }")
                .AddResolver("Query", "foo", ctx =>
                    ctx.Variables.GetVariable<string>("abc"))
                .Create();

            IReadOnlyQueryRequest request = QueryRequestBuilder.New()
                .SetQuery("query abc($def: String){ foo(bar: $def) }")
                .SetVariableValue("def", "ghi")
                .Create();

            // act
            IExecutionResult result =
                await schema.MakeExecutable().ExecuteAsync(request);

            // assert
            result.MatchSnapshot();
        }

        [Fact]
        public async Task CollectFields()
        {
            // arrange
            var list = new List<IFieldSelection>();

            ISchema schema = SchemaBuilder.New()
                .AddDocumentFromString(
                    @"
                    type Query {
                        foo: Foo
                    }

                    type Foo {
                        bar: Bar
                    }

                    type Bar {
                        baz: String
                    }")
                .Use(next => context =>
                {
                    if (context.Field.Type.NamedType() is ObjectType type)
                    {
                        foreach (IFieldSelection selection in context.GetSelections(type))
                        {
                            CollectSelections(context, selection, list);
                        }
                    }
                    return default(ValueTask);
                })
                .Create();

            // act
            await schema.MakeExecutable().ExecuteAsync(
            @"{
                foo {
                    bar {
                        baz
                    }
                }
            }");

            // assert
            list.Select(t => t.Selection.Name.Value).ToList().MatchSnapshot();
        }

        private static void CollectSelections(
            IResolverContext context,
            IFieldSelection selection,
            ICollection<IFieldSelection> collected)
        {
            if (selection.Field.Type.IsLeafType())
            {
                collected.Add(selection);
            }

            if (selection.Field.Type.NamedType() is ObjectType objectType)
            {
                foreach (IFieldSelection child in context.GetSelections(
                    objectType, selection.Selection.SelectionSet))
                {
                    CollectSelections(context, child, collected);
                }
            }
        }
    }
}

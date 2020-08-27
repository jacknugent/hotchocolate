using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionScopeTests
    {
        [Fact]
        public void FilterConvention_Should_Work_When_ConfiguredWithAttributes()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention<IFilterConvention, BarFilterConvention>("Bar")
                .AddQueryType<Query1>()
                .UseFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Work_When_ConfiguredWithType()
        {
            // arrange
            // act
            ISchema schema = SchemaBuilder.New()
                .AddConvention<IFilterConvention, BarFilterConvention>("Bar")
                .AddQueryType<QueryType>()
                .UseFiltering()
                .Create();

            // assert
            schema.ToString().MatchSnapshot();
        }

        public class QueryType : ObjectType
        {
            protected override void Configure(IObjectTypeDescriptor descriptor)
            {
                descriptor.Field("foos").Resolver(new Foo[0].AsQueryable()).UseFiltering();
                descriptor.Field("foosBar").Resolver(new Foo[0].AsQueryable()).UseFiltering("Bar");
            }
        }

        public class Query1
        {
            [UseFiltering]
            public IQueryable<Foo> Foos() => new Foo[0].AsQueryable();

            [UseFiltering(Scope = "Bar")]
            public IQueryable<Foo> FoosBar() => new Foo[0].AsQueryable();
        }

        public class BarFilterConvention : FilterConvention
        {
            protected override void Configure(IFilterConventionDescriptor descriptor)
            {
                descriptor.UseDefault();
                descriptor.Operation(DefaultOperations.Equals).Name("EQUALS");
            }
        }


        public class TestOperationType : StringOperationInput
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Operation(DefaultOperations.Equals).Type<StringType>();
                descriptor.AllowAnd(false).AllowOr(false);
            }
        }

        public class FailingCombinator
            : FilterOperationCombinator<string
            , FilterVisitorContext<string>>
        {
            public override bool TryCombineOperations(
                FilterVisitorContext<string> context,
                Queue<string> operations,
                FilterCombinator combinator,
                [NotNullWhen(true)] out string combined)
            {
                throw new NotImplementedException();
            }
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar);
                descriptor.UseAnd(false).UseOr(false);
            }
        }
    }
}
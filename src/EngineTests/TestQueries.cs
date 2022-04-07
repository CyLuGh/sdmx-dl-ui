using Xunit;
using System;
using System.IO;
using sdmx_dl_engine;
using LanguageExt.UnitTesting;
using FluentAssertions;

namespace EngineTests
{
    public class TestQueries : IDisposable
    {
        public TestQueries()
        {
            File.WriteAllLines( Path.Combine( AppDomain.CurrentDomain.BaseDirectory , "sdmx-dl.properties" ) , new[] { "enableRngDriver=true" } );
        }

        public void Dispose()
        {
            File.Delete( Path.Combine( AppDomain.CurrentDomain.BaseDirectory , "sdmx-dl.properties" ) );
        }

        [Fact]
        public void TestListSources()
        {
            var sources = new Engine().ListSources();
            sources.ShouldBeRight( s => s.Find( x => x.Name.Equals( "RNG" ) ).ShouldBeSome() );
        }

        [Fact]
        public void TestListFlows()
        {
            var flows = new Engine().ListFlows( "RNG" );
            flows.ShouldBeRight( f => f.Find( x => x.Label.Equals( "RNG" ) ).ShouldBeSome() );
        }

        [Fact]
        public void TestListConcepts()
        {
            var concepts = new Engine().ListConcepts( "RNG" , "all:RNG(latest)" );
            concepts.ShouldBeRight( c =>
            {
                c.Length.Should().Be( 2 );
                c.Find( x => x.Label.Equals( "Index" ) && x.Concept.Equals( "INDEX" ) ).ShouldBeSome();
                c.Find( x => x.Label.Equals( "Frequency" ) && x.Concept.Equals( "FREQ" ) ).ShouldBeSome();
            } );
        }

        [Fact]
        public void TestListCodes()
        {
            var engine = new Engine();
            var frequencies = engine.ListCodes( "RNG" , "all:RNG(latest)" , "FREQ" );
            frequencies.ShouldBeRight( f =>
            {
                f.Length.Should().Be( 3 );
                f.Find( x => x.Code.Equals( "M" ) ).ShouldBeSome();
            } );
        }
    }
}
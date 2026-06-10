namespace Grimoire.Tests.Application;

using Grimoire.Application.Export;
using Xunit;

public sealed class SectionTypeUtilTests {
	[Theory]
	[InlineData("intro", BookSection.Intro)]
	[InlineData("INTRO", BookSection.Intro)]
	[InlineData("Intro", BookSection.Intro)]
	[InlineData("intropage", BookSection.IntroPage)]
	[InlineData("INTROPAGE", BookSection.IntroPage)]
	[InlineData("description", BookSection.Description)]
	[InlineData("DESCRIPTION", BookSection.Description)]
	[InlineData("content", BookSection.Content)]
	[InlineData("chapters", BookSection.Chapters)]
	[InlineData("toc", BookSection.Toc)]
	[InlineData("tableofcontents", BookSection.TableOfContents)]
	[InlineData("TABLEOFCONTENTS", BookSection.TableOfContents)]
	public void FromString_KnownValues_ReturnCorrectSection(string input, BookSection expected) {
		Assert.Equal(expected, SectionTypeUtil.FromString(input));
	}

	[Theory]
	[InlineData("unknown")]
	[InlineData("")]
	[InlineData("random-garbage")]
	[InlineData("introduction")]
	public void FromString_UnknownValues_ReturnUnknown(string input) {
		Assert.Equal(BookSection.Unknown, SectionTypeUtil.FromString(input));
	}
}

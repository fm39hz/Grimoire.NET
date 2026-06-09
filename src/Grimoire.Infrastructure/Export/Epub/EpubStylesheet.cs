namespace Grimoire.Infrastructure.Export.Epub;

/// <summary>
///     Provides the CSS stylesheet for EPUB styling
/// </summary>
public static class EpubStylesheet {
	public const string DEFAULT_CSS = @"
@namespace epub ""http://www.idpf.org/2007/ops"";

body {
    margin: 0;
    padding: 5px;
    text-align: justify;
    line-height: 1.4em;
    font-family: serif;
}

h1, h2, h3 {
    text-align: center;
    margin: 1em 0;
    font-weight: bold;
}

p {
    margin-bottom: 1em;
    text-indent: 1em;
}

/* Title Page */
.title-page {
    text-align: center;
    margin-top: 5vh;
}

.book-title {
    font-size: 2em;
    font-weight: bold;
    margin-bottom: 0.5em;
}

.book-author {
    font-size: 1.2em;
    font-style: italic;
    margin-bottom: 2em;
}

.book-cover img {
    max-height: 50vh;
    max-width: 100%;
}

.tags {
    margin-top: 2em;
    font-size: 0.8em;
}

.tag-item {
    display: inline-block;
    border: 1px solid #ddd;
    padding: 2px 8px;
    border-radius: 4px;
    margin: 2px;
}

/* Front Matter */
.front-matter {
    margin-top: 2em;
}

.section-title {
    font-weight: bold;
    margin-top: 1.5em;
    border-left: 4px solid #333;
    padding-left: 10px;
}

.text-justify {
    text-align: justify;
}

.center {
    text-align: center;
}

/* Images */
img {
    display: block;
    margin: 10px auto;
    max-width: 100%;
    height: auto;
}

/* Table of Contents */
nav#toc ol {
    list-style-type: none;
    padding-left: 0;
}

nav#toc > ol > li {
    margin-top: 1em;
    font-weight: bold;
}

nav#toc > ol > li > ol {
    list-style-type: none;
    padding-left: 1.5em;
    font-weight: normal;
}

nav#toc > ol > li > ol > li {
    margin-top: 0.5em;
}

nav#toc a {
    text-decoration: none;
}

nav#toc a:hover {
    text-decoration: underline;
}

/* Footnotes */
a.footnote-link {
    vertical-align: super;
    font-size: 0.75em;
    text-decoration: none;
    margin-left: 2px;
}

aside.footnote-content {
    margin-top: 1em;
    padding: 0.5em;
    border-top: 1px solid #ccc;
    font-size: 0.9em;
    display: block;
}

aside.footnote-content p {
    margin: 0;
    text-indent: 0;
}

aside.footnote-content div.note-header {
    font-weight: bold;
    margin-bottom: 0.5em;
}

/* Image containers */
.img-container {
    text-align: center;
    page-break-inside: avoid;
    margin: 1.5em 0;
}

.img-container img {
    max-width: 100%;
    height: auto;
}

.img-caption {
    font-size: 0.9em;
    font-style: italic;
    text-align: center;
    margin-top: 0.5em;
}

/* Divider */
hr.divider {
    border: 0;
    text-align: center;
    margin: 2em 0;
}

hr.divider::before {
    content: ""* * *"";
    font-weight: bold;
    letter-spacing: 0.5em;
}

/* Text formatting */
strong {
    font-weight: bold;
}

em {
    font-style: italic;
}

/* No select */
.no-select {
    -webkit-user-select: none;
    -moz-user-select: none;
    -ms-user-select: none;
    user-select: none;
}

/* Endnotes */
.endnotes {
    margin: 1em 0;
}

.endnotes h3 {
    margin-top: 1.5em;
    padding-bottom: 0.3em;
    border-bottom: 1px solid #ccc;
    font-size: 1.1em;
}

.endnote-entry {
    margin: 0.5em 0;
    font-size: 0.9em;
    text-indent: 0;
}

.endnote-backref {
    text-decoration: none;
    margin-left: 0.3em;
    font-size: 0.85em;
}

a.footnote-ref[id] {
    scroll-margin-top: 2em;
}

/* Dropcap */
.dropcap {
    float: left;
    font-size: 3.2em;
    line-height: 0.8em;
    margin-right: 0.15em;
    margin-top: 0.15em;
    font-weight: bold;
}
";
}

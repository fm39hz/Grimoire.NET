# Grimoire

### Ebook Editor & Publisher

> Let's the magic flow through your books

---

[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![MIT License][license-shield]][license-url]

---

**Grimoire** is a Digital Archiving & Publishing System designed to manage, edit, and publish digital books. Built with a local-first philosophy and a unified book-tree structure, Grimoire enables seamless organization and high-fidelity compilation of your manuscripts.

---

## Table of Contents

- [About The Project](#about-the-project)
    - [Key Features](#key-features)
    - [Built With](#built-with)
- [Getting Started](#getting-started)
    - [Prerequisites](#prerequisites)
    - [Installation & Run](#installation--run)
- [Usage](#usage)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
- [Acknowledgments](#acknowledgments)

---

## About The Project

GRIMOIRE provides an elegant, structured environment for digital archives. Whether you manage a large digital library or compile individual novels, it delivers absolute precision from layout styling to metadata management.

### Key Features

- **High-Fidelity Storage:** Stores book structures (Series/Volume/Chapter) and content segments (Text/Image/Divider) with absolute precision.
- **Unified Book Tree:** Structural management separated from heavy content payloads, utilizing fractional ordering algorithms for single-update insertions.
- **Scriban Templates:** Fast, robust document packaging through the Scriban templating engine.
- **Masterful Binding:** Packages multiple volumes and chapters into commercial-grade EPUB 3.3 formats (Kindle/Apple Books compatible) with full popup footnote support.
- **Flexible Storage:** Local Storage or S3-compatible object storage (e.g., MinIO).

### Built With

- [![.NET 10][dotnet-shield]][dotnet-url]
- [![PostgreSQL][postgres-shield]][postgres-url]
- [![EF Core 10][efcore-shield]][efcore-url]
- [![Docker][docker-shield]][docker-url]
- [![Hangfire][hangfire-shield]][hangfire-url]

---

## Getting Started

Follow these steps to set up and run Grimoire on your local machine.

### Prerequisites

- [Docker & Docker Compose](https://www.docker.com/)
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (optional, only for local IDE-based development)

### Installation & Run

1. Clone the repository:

    ```sh
    git clone https://github.com/fm39hz/Grimoire.NET.git
    ```

2. Start the services using Docker Compose:

    ```sh
    docker compose up --build
    ```

3. Access the OpenAPI Dashboard at:
    ```
    http://localhost:8080/scalar/v1
    ```

---

## Usage

Grimoire structures your publishing workflows into three primary modules:

- **The Collector:** Coordinates asset uploads (Covers, Content Images) and imports raw chapter data from scraper scripts.
- **The Librarian:** Manages metadata for series (Authors, Artists, Description, Tags) and volumes (ISBN, Publication Date).
- **The Bindery:** Compiles collections into beautiful digital books under background jobs.

For detailed API definitions, refer to the [API Documentation](doc/03-api-features.md).

---

## Roadmap

- [x] Series/Volume/Chapter CRUD operations
- [x] S3 Storage & LocalStorage options
- [x] Scriban HTML/Markdown/EPUB Render Engine
- [x] Hangfire Background Export Jobs
- [x] Chapter Split & Merge APIs
- [ ] AZW3 Export Implementation
- [ ] Advanced Search & Tag Filter

See the [Open Issues](https://github.com/fm39hz/Grimoire.NET/issues) for a full list of proposed features.

---

## Contributing

Contributions are what make the open-source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/AmazingFeature`)
3. Commit your Changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the Branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

---

## License

Distributed under the MIT License. See `LICENSE` for more information.

---

## Contact

**Project Lead:** [FM39hz](https://fm39hz.online)

**Project Link:** [https://github.com/fm39hz/Grimoire.NET](https://github.com/fm39hz/Grimoire.NET)

---

## Acknowledgments

- [VersOne.Epub](https://github.com/versone/Epub) - Outstanding EPUB parsing library.
- [Scriban](https://github.com/scriban/scriban) - Extremely fast and lightweight templating tool.
- [Hangfire](https://www.hangfire.io/) - Reliable background processing for .NET.

[Back to top](#grimoire)

<!-- MARKDOWN LINKS & IMAGES -->

[contributors-shield]: https://img.shields.io/github/contributors/fm39hz/Grimoire.NET.svg?style=for-the-badge
[contributors-url]: https://github.com/fm39hz/Grimoire.NET/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/fm39hz/Grimoire.NET.svg?style=for-the-badge
[forks-url]: https://github.com/fm39hz/Grimoire.NET/network/members
[stars-shield]: https://img.shields.io/github/stars/fm39hz/Grimoire.NET.svg?style=for-the-badge
[stars-url]: https://github.com/fm39hz/Grimoire.NET/stargazers
[issues-shield]: https://img.shields.io/github/issues/fm39hz/Grimoire.NET.svg?style=for-the-badge
[issues-url]: https://github.com/fm39hz/Grimoire.NET/issues
[license-shield]: https://img.shields.io/github/license/fm39hz/Grimoire.NET.svg?style=for-the-badge
[license-url]: https://github.com/fm39hz/Grimoire.NET/blob/master/LICENSE
[dotnet-shield]: https://img.shields.io/badge/.NET%2010-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[dotnet-url]: https://dotnet.microsoft.com/
[postgres-shield]: https://img.shields.io/badge/PostgreSQL-4169E1?style=for-the-badge&logo=postgresql&logoColor=white
[postgres-url]: https://www.postgresql.org/
[efcore-shield]: https://img.shields.io/badge/EF%20Core-512BD4?style=for-the-badge&logo=dotnet&logoColor=white
[efcore-url]: https://learn.microsoft.com/ef/core/
[docker-shield]: https://img.shields.io/badge/Docker-2496ED?style=for-the-badge&logo=docker&logoColor=white
[docker-url]: https://www.docker.com/
[hangfire-shield]: https://img.shields.io/badge/Hangfire-D22630?style=for-the-badge&logo=buffer&logoColor=white
[hangfire-url]: https://www.hangfire.io/

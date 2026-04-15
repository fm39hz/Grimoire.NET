# Grimoire.NET

> **A Digital Archiving & Publishing System designed for managing and publishing digital books**

[Explore the docs](https://github.com/fm39hz/Grimoire.NET/doc)
[Report bug](https://github.com/fm39hz/Grimoire.NET/issues)
[Request Feature](https://github.com/fm39hz/Grimoire.NET/issues)

## Table of content

<!--toc:start-->

- [Table of content](#table-of-content)
- [About The Project](#about-the-project)
  - [Key Features](#key-features)
- [Getting Started](#getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
- [Usage](#usage)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
  <!--toc:end-->

## About The Project

**GRIMOIRE** is a Digital Archiving & Publishing System designed for managing and publishing digital books. It focuses on high-fidelity storage of book structures (Series/Volume/Chapter) and content (Text/Image), professional metadata management, and masterful binding into EPUB 3.3 format compatible with commercial readers like Kindle and Apple Books.

### Key Features

- **High-Fidelity Storage**: Precise storage of book structures and content.
- **Professional Metadata**: Detailed management of authors, artists, tags, and covers.
- **Masterful Binding**: Export to EPUB 3.3 standard with beautiful layouts.
- **Anthology Support**: Package multiple volumes into a single "Complete Collection".
- **Local-First**: Designed for local operation with manual curation.

## Getting Started

To get a local copy up and running, follow these steps.

### Prerequisites

- Docker & Docker Compose
- .NET 10 SDK (for local development)

### Installation

1. Clone the repo

    ```sh
    git clone https://github.com/fm39hz/Grimoire.NET.git
    ```

2. Deployment with Docker

    ```sh
    docker compose up --build
    ```

3. Access the API at `http://localhost:5062`

## Usage

Grimoire allows you to manage the entire lifecycle of a digital book:

- **The Collector**: Handles data import (asset uploads, chapter imports).
- **The Librarian**: Manages metadata updates for series and volumes.
- **The Bindery**: Processes EPUB export jobs.

_For more details, please refer to the [Documentation](https://github.com/fm39hz/Grimoire.NET/doc)_

## Roadmap

- [x] Series/Volume/Chapter CRUD operations
- [x] Asset management and storage
- [x] Basic metadata handling
- [x] EPUB 3.3 export
- [ ] PDF export implementation
- [ ] Background job processing
- [ ] Search and filtering

See the [open issues](https://github.com/fm39hz/Grimoire.NET/issues) for a full list of proposed features.

## Contributing

Contributions are what make the open source community such an amazing place to learn, inspire, and create. Any contributions you make are **greatly appreciated**.

1. Fork the Project
2. Create your Feature Branch (`git checkout -b feature/amazing-feature`)
3. Commit your Changes (`git commit -m 'Add some amazing feature'`)
4. Push to the Branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## License

Distributed under the MIT License. See `LICENSE` for more information.

## Contact

[FM39hz](hitpoint2k3@gmail.com)

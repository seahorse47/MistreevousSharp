# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2025-11-22

### Added
- README.md is now included in the NuGet package

## [1.0.0] - 2025-11-22

### Added
- Initial release of MistreevousSharp
- Support for MDSL and JSON definitions
- Composite nodes: Sequence, Selector, Parallel, Race, All, Lotto
- Decorator nodes: Root, Repeat, Retry, Flip, Succeed, Fail
- Leaf nodes: Action, Condition, Wait, Branch
- Callbacks: Entry, Exit, Step
- Guards: While, Until
- Support for async actions
- Global functions and subtrees
- Support for .NET 9.0 and .NET Standard 2.1


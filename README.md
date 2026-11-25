# MAC-E (MAC-ENCRYPTION)

### Device-Aware File Encryption and Collaborative Access

**MAC-E** is a cross-platform file encryption prototype developed using C# and .NET MAUI. It explores a device-based approach to file access control, allowing multiple registered devices to access and edit the same encrypted file.

Rather than treating file access as dependent solely on a shared password, MAC-E explores associating encrypted files with a set of authorized devices.

---

## 1. Project Overview

The central idea behind MAC-E is to make access to encrypted files dependent on device registration.

When encrypting a file, the owner can register the devices that are permitted to access it. Any device registered with that encrypted file can open and edit its contents.

The application is designed around three main concepts:

- Device-based access authorization.
- Collaborative editing across registered devices.
- Temporary decryption to avoid maintaining a separate, permanently decrypted copy of the file.

## 2. Core Features

### File Encryption
- Encrypt files using the application's cryptographic services.
- Store encrypted files using the custom `.mace` extension.
- Associate encrypted files with their authorized-device information.

### Device Registration
- Maintain device identity information.
- Register devices for access to encrypted files.
- Restrict file access according to the devices registered with the file.

### Collaborative Editing
- Allow any registered device to open and edit an encrypted file.
- Support access to the same encrypted file across multiple authorized devices.
- Preserve the encrypted-file workflow while allowing authorized users to work with its contents.

### Temporary Decryption
- Decrypt files temporarily when an authorized device opens them.
- Provide the decrypted content for editing.
- Intend to remove the temporary decrypted copy after the editing session ends.

---

## 3. Technology Stack

| Technology | Purpose |
|---|---|
| C# | Application development |
| .NET MAUI | Cross-platform application framework |
| XAML | User interface development |
| MVVM | Application architecture |
| Cryptographic APIs | Encryption and decryption |
| Git & GitHub | Version control and source-code management |

## 4. Project Architecture

The application follows a modular architecture, separating application logic, user interface components, data models and platform-specific functionality.

Main architectural components include:

- **Models:** Represent application data and entities.
- **Services:** Handle encryption, device identity, file operations and related functionality.
- **ViewModels:** Manage presentation logic using the MVVM pattern.
- **Views:** Contain the application's user interface.
- **Platforms:** Contain platform-specific implementations for supported operating systems.

## 5. Project Structure

```text
MAC-Encryption/
│
├── MAC-E.slnx
│
└── src/
    └── MacE/
        ├── Models/
        ├── Services/
        ├── ViewModels/
        ├── Views/
        └── Platforms/
```

## 6. Getting Started

### Prerequisites

- Windows operating system.
- Visual Studio with the required .NET MAUI workload.
- Compatible .NET SDK.
- Git.

### Installation

**1. Clone the repository**

```bash
git clone https://github.com/krishnaadsod7-sketch/MAC-Encryption.git
```

**2. Open the project**

Open `MAC-E.slnx` in Visual Studio.

**3. Restore dependencies**

Allow Visual Studio to restore the required NuGet packages.

**4. Build and run**

Select the appropriate target platform and build the application.

---

## 7. Project Scope

This repository represents the completed experimental prototype of MAC-E.

The project was developed to explore the feasibility of device-aware file encryption, registered-device authorization, collaborative editing and recovery through trusted users.

The prototype demonstrates the initial implementation of these concepts and is preserved in its current state.

It is not presented as a production-ready security product.

## 8. Project Status

This project has been declared completed.

## 9. Author

**Krishna Adsod**

GitHub: [@krishnaadsod7-sketch](https://github.com/krishnaadsod7-sketch)

**Repository:** [MAC-Encryption](https://github.com/krishnaadsod7-sketch/MAC-Encryption)

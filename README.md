# Metaballs in Unity URP with Custom Render Passes

This project demonstrates how to implement a metaball effect in Unity using the Universal Render Pipeline (URP).

The goal is to achieve the classic "lava lamp" effect by drawing metaballs with smooth gradients and semi-transparent edges, using a custom shader and render passes.
This approach avoids the traditional blur method, optimizing for performance, especially on lower-end devices.

## Details of implementation

You can find a blog post that I wrote about [here](https://mayonesso.com/blog/2024/09/metaballs/).

## Installation

To use this project, follow the steps below:

1. **Clone the repository**:

   ```bash
   git clone https://github.com/your-username/metaballs-urp.git
   ```

2. **Open in Unity**:

- Ensure you have Unity 6.
  
- Open the project folder with Unity Hub.

## Performance Considerations

This method is more performance-friendly than blur-based metaball implementations, especially for mobile platforms.
Ensure that your URP settings are optimized for mobile rendering if targeting such devices.

## License

This project is open-source and licensed under the MIT License. Feel free to modify and use it in your own projects.

import { Provider } from "react-redux";
import { RouterProvider } from "react-router-dom";
import appRouter from "./screens/router";
import { store } from "./store/store";
import configureAxios from "./services/axios";

function App() {
  configureAxios();

  return (
    <Provider store={store}>
      <RouterProvider router={appRouter} />
    </Provider>
  );
}

export default App;

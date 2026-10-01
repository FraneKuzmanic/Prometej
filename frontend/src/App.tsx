import { useEffect } from "react";
import { Provider } from "react-redux";
import { RouterProvider } from "react-router-dom";
import appRouter from "./screens/router";
import { store } from "./store/store";
import configureAxios from "./services/axios";
import { fetchCurrentUser } from "./store/slices/userSlice";

configureAxios();

function App() {
  // The session lives in an HttpOnly cookie the page cannot read, so ask the server who we are.
  useEffect(() => {
    store.dispatch(fetchCurrentUser());
  }, []);

  return (
    <Provider store={store}>
      <RouterProvider router={appRouter} />
    </Provider>
  );
}

export default App;

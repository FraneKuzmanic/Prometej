import logo from "/logo.svg";
import { Art, ArtText } from "./index.styled";

// The half of the sign-in and registration screens that is not the form.
export default function AuthArt() {
  return (
    <Art>
      <img className="painting" src="/prometej.png" alt="" />
      <ArtText>
        <img src={logo} alt="" />
        <div>
          <h2>Prometej</h2>
          <p>Plamen znanja hrvatske književnosti</p>
        </div>
      </ArtText>
    </Art>
  );
}

Feature: Navigation Bar

  As a visitor to the website
  I want to easily access the pages
  So that I can navigate the site and understand how my data is handled

  Scenario: Navigation bar provides access to Home and Privacy
    Given the home page is opened
    Then the "Home" navigation button should be visible
    And the "Privacy" navigation button should be visible
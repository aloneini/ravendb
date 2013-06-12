/// <reference path="../main.js" />
/*global angular:false */
'use strict';

clusterManagerApp.controller('apiCtrl', function apiCtrl($scope, $http) {

    $scope.getNewKey = function () {
        var newApiKey = "";
        var possible = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        for (var i = 0; i < 22; i++)
            newApiKey += possible.charAt(Math.floor(Math.random() * possible.length));

        return newApiKey;

    };

    $scope.generateSecret = function () {

        $scope.Secret = $scope.getNewKey();
    };

    $scope.doesNotHaveName = function () {
        return !$scope.name;
    };


    $scope.saveApiKeys = function () {
        
        if ($scope.name == null || $scope.Secret == null) {
            return;
        }
        
        $scope.FullApiKey = $scope.name + '/' + $scope.Secret;
        var countServer = 0;
        var apiKeysToSave = []; // Array of class ApiKeyViewModel
        _.forEach($scope.stats.servers, function (value, index, array) {
            if (!value.serverCheckbox)
                return;

            apiKeysToSave.push({ Secret: value.Secret, Name: value.name, Server: value.Id, Enable:$scope.Enable11 });// If checked then get info
            countServer += 1;// Checking if any server was signed
        });
        
        if (countServer == 0) {
            $scope.alert = "* Please choose a server";
            return; // If no server was checked than return
        }
        if (countServer != 0)
            $scope.alert = "";// In case the user signed a textbox
        
        $http.post("/api/servers/save-api", apiKeysToSave)
            .success(function (result) {
                alert('Done');
            });
    };


});